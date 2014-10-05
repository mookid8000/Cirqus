using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using d60.Cirqus.Events;
using d60.Cirqus.Exceptions;
using d60.Cirqus.Extensions;
using d60.Cirqus.Numbers;
using d60.Cirqus.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;

namespace d60.Cirqus.NTFS.Events
{
    /// <summary>
    /// *Prototype* of event store that saves events directly to files for use in embedded scenarios. 
    /// Only supports a single process using the store at a time. 
    /// Uses filesystem's index for easy lookup of single aggregate streams.
    /// Uses a single file for global sequence index.
    /// Uses a single file for commit log.
    /// 
    /// todo:
    /// - Cache global sequence number
    /// - Don't make load/stream wait for writes, read to last commit
    /// </summary>
    public class NtfsEventStore : IEventStore, IDisposable
    {
        internal const int SizeofSeqRecord = 32;
        internal const int SizeofCommitRecord = sizeof(long);

        readonly object _lock = new object();

        readonly JsonSerializer _serializer;

        readonly string _dataDirectory;
        readonly string _seqFilePath;
        readonly string _commitsFilePath;
        readonly FileStream _commitsReadStream;
        readonly BinaryReader _commitsReader;

        public NtfsEventStore(string basePath, bool dropEvents = false)
        {
            _serializer = CreateSerializer();

            _dataDirectory = Path.Combine(basePath, "events");
            _seqFilePath = Path.Combine(basePath, "seq.idx");
            _commitsFilePath = Path.Combine(basePath, "commits.idx");

            if (dropEvents) DropEvents();

            Directory.CreateDirectory(_dataDirectory);

            SeqWriter = new BinaryWriter(
                new FileStream(_seqFilePath, FileMode.Append, FileSystemRights.AppendData, FileShare.Read, 1024, FileOptions.None), 
                Encoding.ASCII, leaveOpen: false);

            CommitsWriter = new BinaryWriter(
                new FileStream(_commitsFilePath, FileMode.Append, FileSystemRights.AppendData, FileShare.Read, 1024, FileOptions.None), 
                Encoding.ASCII, leaveOpen: false);

            _commitsReadStream = new FileStream(_commitsFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 1024, FileOptions.None);
            _commitsReader = new BinaryReader(_commitsReadStream, Encoding.ASCII);
        }

        internal BinaryWriter SeqWriter { get; private set; }
        internal BinaryWriter CommitsWriter { get; private set; }

        public void Save(Guid batchId, IEnumerable<DomainEvent> batch)
        {
            lock (_lock)
            {
                var globalSequenceNumber = ReadGlobalSequenceNumber();

                var events = batch.ToList();

                foreach (var domainEvent in events)
                {
                    domainEvent.Meta[DomainEvent.MetadataKeys.GlobalSequenceNumber] = ++globalSequenceNumber;
                    domainEvent.Meta[DomainEvent.MetadataKeys.BatchId] = batchId;
                }

                EventValidation.ValidateBatchIntegrity(batchId, events);

                foreach (var domainEvent in events)
                {
                    WriteEventToSeqIndex(globalSequenceNumber, batchId, domainEvent);
                }

                SeqWriter.Flush();

                foreach (var domainEvent in events)
                {
                    try
                    {
                        // write the data
                        WriteEvent(domainEvent);
                    }
                    catch (IOException exception)
                    {
                        throw new ConcurrencyException(batchId, events, exception);
                    }
                }

                // commit the batch
                CommitsWriter.Write(globalSequenceNumber);
                CommitsWriter.Write(globalSequenceNumber); // "checksum"
                CommitsWriter.Flush();
            }
        }

        public IEnumerable<DomainEvent> Load(Guid aggregateRootId, long firstSeq = 0, long limit = Int64.MaxValue)
        {
            var currentGlobalSequenceNumber = ReadGlobalSequenceNumber();

            var aggregateDirectory = Path.Combine(_dataDirectory, aggregateRootId.ToString());
            
            if (!Directory.Exists(aggregateDirectory))
                return Enumerable.Empty<DomainEvent>();

            return from path in Directory.EnumerateFiles(aggregateDirectory)
                   let seq = int.Parse(Path.GetFileName(path))
                   where seq >= firstSeq && seq < firstSeq + limit
                   let @event = ReadEvent(path)
                   where @event != null && @event.GetGlobalSequenceNumber(true) <= currentGlobalSequenceNumber
                   select @event;
        }

        public IEnumerable<DomainEvent> Stream(long globalSequenceNumber = 0)
        {
            var currentGlobalSequenceNumber = ReadGlobalSequenceNumber();

            using (var seqReadStream = new FileStream(_seqFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 100*SizeofSeqRecord, FileOptions.None))
            using (var seqReader = new BinaryReader(seqReadStream, Encoding.ASCII))
            {
                // todo: seek from begin/end whichever is closest
                // todo: is the readstream changed as more is written to the file?
                seqReadStream.Seek(globalSequenceNumber*SizeofSeqRecord, SeekOrigin.Begin);

                // todo: don't read longer than last known good commit (writer might be working concurrently here)
                while (seqReadStream.Position < seqReadStream.Length)
                {
                    var record = ReadGlobalSequenceRecord(seqReader);

                    if (record.GlobalSequenceNumber > currentGlobalSequenceNumber)
                        break;

                    var dataFileName = Path.Combine(_dataDirectory, record.AggregateRootId.ToString(), GetFilename(record.LocalSequenceNumber));
                    yield return ReadEvent(dataFileName);
                }
            }
        }

        public long GetNextGlobalSequenceNumber()
        {
            return ReadGlobalSequenceNumber() + 1;
        }

        long ReadGlobalSequenceNumber()
        {
            if (_commitsReadStream.Length == 0) return -1;
            
            _commitsReadStream.Seek(0, SeekOrigin.End);

            var garbage = _commitsReadStream.Length%SizeofCommitRecord;
            if (garbage > 0)
            {
                // we have a failed commit on our hands, skip the garbage
                _commitsReadStream.Seek(-garbage, SeekOrigin.End);
            }

            // read commit and checksum
            if (_commitsReadStream.Length < garbage + SizeofCommitRecord*2) return -1;
            _commitsReadStream.Seek(-SizeofCommitRecord*2, SeekOrigin.Current);
            var globalSequenceNumber = _commitsReader.ReadInt64();
            var checksum = _commitsReader.ReadInt64();

            if (globalSequenceNumber == checksum)
                return globalSequenceNumber;

            // ok, the checksum was never written, skip the orphaned commit and try again
            if (_commitsReadStream.Length < garbage + SizeofCommitRecord*3) return -1;
            _commitsReadStream.Seek(-SizeofCommitRecord*3, SeekOrigin.Current);
            globalSequenceNumber = _commitsReader.ReadInt64();
            checksum = _commitsReader.ReadInt64();

            if (globalSequenceNumber == checksum)
                return globalSequenceNumber;

            throw new InvalidOperationException("Commit file is unreadable.");
        }

        void MaybeRecoverFromFailure(long globalSequenceNumber)
        {
            if (globalSequenceNumber == SeqWriter.BaseStream.Length / SizeofSeqRecord)
                return;

            using (var seqReadStream = new FileStream(_seqFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 100 * SizeofSeqRecord, FileOptions.None))
            using (var seqReader = new BinaryReader(seqReadStream, Encoding.ASCII))
            {
                seqReadStream.Seek(globalSequenceNumber * SizeofSeqRecord, SeekOrigin.Begin);

                // todo: don't read past end of file
                // todo: check the validity of each record with checksum 
                while (seqReadStream.Position < seqReadStream.Length)
                {
                    var record = ReadGlobalSequenceRecord(seqReader);

                    var dataFileName = Path.Combine(_dataDirectory, record.AggregateRootId.ToString(), GetFilename(record.LocalSequenceNumber));
                    if (File.Exists(dataFileName))
                        File.Delete(dataFileName);
                }
            }

            SeqWriter.BaseStream.SetLength(globalSequenceNumber * SizeofSeqRecord);
            SeqWriter.BaseStream.Flush();
        }

        internal void WriteEventToSeqIndex(long globalSequenceNumber, Guid batchId, DomainEvent domainEvent)
        {
            var aggregateRootId = domainEvent.GetAggregateRootId();
            var sequenceNumber = domainEvent.GetSequenceNumber();

            // write the sequence
            SeqWriter.Write(globalSequenceNumber); //todo: wrong number here
            SeqWriter.Write(aggregateRootId.ToByteArray());
            SeqWriter.Write(sequenceNumber);
        }

        static GlobalSequenceRecord ReadGlobalSequenceRecord(BinaryReader seqReader)
        {
            return new GlobalSequenceRecord
            {
                GlobalSequenceNumber = seqReader.ReadInt64(),
                AggregateRootId = new Guid(seqReader.ReadBytes(16)),
                LocalSequenceNumber = seqReader.ReadInt64(),
            };
        }

        DomainEvent ReadEvent(string filename)
        {
            using (var fileStream = new FileStream(filename, FileMode.Open))
            using (var bsonReader = new BsonReader(fileStream))
            {
                if (fileStream.Length == 0)
                    return null;

                try
                {
                    return _serializer.Deserialize<DomainEvent>(bsonReader);
                }
                catch (Exception)
                {
                    return null;
                }
            }
        }

        internal void WriteEvent(DomainEvent domainEvent)
        {
            var aggregateRootId = domainEvent.GetAggregateRootId();
            var sequenceNumber = domainEvent.GetSequenceNumber();

            var aggregateDirectory = Path.Combine(_dataDirectory, aggregateRootId.ToString());
            Directory.CreateDirectory(aggregateDirectory);

            var filename = Path.Combine(aggregateDirectory, GetFilename(sequenceNumber));

            using (var fileStream = new FileStream(filename, FileMode.CreateNew, FileAccess.Write, FileShare.None, 1024, FileOptions.None))
            using (var bsonWriter = new BsonWriter(fileStream))
            {
                _serializer.Serialize(bsonWriter, domainEvent);
            }
        }

        static JsonSerializer CreateSerializer()
        {
            var binder = new TypeAliasBinder("<events>");
            var serializer = new JsonSerializer
            {
                Binder = binder.AddType(typeof (Metadata)),
                TypeNameHandling = TypeNameHandling.Objects,
                Formatting = Formatting.Indented
            };
            return serializer;
        }

        static string GetFilename(long sequenceNumber)
        {
            return sequenceNumber.ToString(CultureInfo.InvariantCulture).PadLeft(20, '0');
        }

        void DropEvents()
        {
            if (!Directory.Exists(_dataDirectory))
                return;

            foreach (var file in new DirectoryInfo(_dataDirectory).GetFiles()) file.Delete();
            foreach (var dir in new DirectoryInfo(_dataDirectory).GetDirectories()) dir.Delete(true);

            if (File.Exists(_seqFilePath))
                File.Delete(_seqFilePath);

            if (File.Exists(_commitsFilePath))
                File.Delete(_commitsFilePath);
        }

        public void Dispose()
        {
            SeqWriter.Dispose();
            CommitsWriter.Dispose();
            _commitsReader.Dispose();
            _commitsReadStream.Dispose();
        }

        public class GlobalSequenceRecord
        {
            public long GlobalSequenceNumber { get; set; }
            public Guid AggregateRootId { get; set; }
            public long LocalSequenceNumber { get; set; }
        }
    }
}

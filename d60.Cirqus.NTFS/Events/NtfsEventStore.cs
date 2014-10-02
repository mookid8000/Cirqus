using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
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
        const int sizeofSeqRecord = 56;
        const int sizeofCommitRecord = 8;

        readonly object _lock = new object();

        readonly JsonSerializer _serializer;

        readonly string _dataDirectory;
        readonly string _seqFilePath;
        readonly string _commitsFilePath;
        readonly FileStream _seqWriteStream;
        readonly BinaryWriter _seqWriter;
        readonly FileStream _commitsWriteStream;
        readonly BinaryWriter _commitsWriter;
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

            _seqWriteStream = new FileStream(_seqFilePath, FileMode.Append, FileAccess.Write, FileShare.Read, 1024, FileOptions.None);
            _seqWriter = new BinaryWriter(_seqWriteStream, Encoding.ASCII);

            _commitsWriteStream = new FileStream(_commitsFilePath, FileMode.Append, FileAccess.Write, FileShare.Read, 1024, FileOptions.None);
            _commitsWriter = new BinaryWriter(_commitsWriteStream, Encoding.ASCII);

            _commitsReadStream = new FileStream(_commitsFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 1024, FileOptions.None);
            _commitsReader = new BinaryReader(_commitsReadStream, Encoding.ASCII);
        }

        public void Save(Guid batchId, IEnumerable<DomainEvent> batch)
        {
            lock (_lock)
            {
                var globalSequenceNumber = ReadGlobalSequenceNumber();

                var events = batch.ToList();

                foreach (var domainEvent in events)
                {
                    domainEvent.Meta[DomainEvent.MetadataKeys.GlobalSequenceNumber] = globalSequenceNumber++;
                    domainEvent.Meta[DomainEvent.MetadataKeys.BatchId] = batchId;
                }

                EventValidation.ValidateBatchIntegrity(batchId, events);

                foreach (var domainEvent in events)
                {
                    var aggregateRootId = domainEvent.GetAggregateRootId();
                    var sequenceNumber = domainEvent.GetSequenceNumber();

                    // write the sequence
                    _seqWriter.Write(globalSequenceNumber);
                    _seqWriter.Write(batchId.ToByteArray());
                    _seqWriter.Write(events.Count);
                    _seqWriter.Write(aggregateRootId.ToByteArray());
                    _seqWriter.Write(sequenceNumber);
                    _seqWriter.Write(123456);
                }

                _seqWriter.Flush();

                foreach (var domainEvent in events)
                {
                    var aggregateRootId = domainEvent.GetAggregateRootId();
                    var sequenceNumber = domainEvent.GetSequenceNumber();

                    var aggregateDirectory = Path.Combine(_dataDirectory, aggregateRootId.ToString());
                    Directory.CreateDirectory(aggregateDirectory);

                    try
                    {
                        // write the data
                        WriteEvent(Path.Combine(aggregateDirectory, GetFilename(sequenceNumber)), domainEvent);
                    }
                    catch (IOException exception)
                    {
                        throw new ConcurrencyException(batchId, events, exception);
                    }
                }

                // commit the batch
                // todo: write a checksum to ensure that commit is fully written (maybe just g-seq twice?)
                _commitsWriter.Write(globalSequenceNumber);
                _commitsWriter.Flush();
            }
        }


        public IEnumerable<DomainEvent> Load(Guid aggregateRootId, long firstSeq = 0, long limit = Int32.MaxValue)
        {
            ReadGlobalSequenceNumber();

            var aggregateDirectory = Path.Combine(_dataDirectory, aggregateRootId.ToString());
            
            if (!Directory.Exists(aggregateDirectory))
                return Enumerable.Empty<DomainEvent>();

            // todo: don't read longer than last known good commit (writer might be working concurrently here)
            return from path in Directory.EnumerateFiles(aggregateDirectory)
                   let seq = int.Parse(Path.GetFileName(path))
                   where seq >= firstSeq && seq < firstSeq + limit
                   select ReadEvent(path);
        }

        public IEnumerable<DomainEvent> Stream(long globalSequenceNumber = 0)
        {
            ReadGlobalSequenceNumber();

            using (var seqReadStream = new FileStream(_seqFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 100*sizeofSeqRecord, FileOptions.None))
            using (var seqReader = new BinaryReader(seqReadStream, Encoding.ASCII))
            {
                seqReadStream.Seek(globalSequenceNumber*sizeofSeqRecord, SeekOrigin.Begin);

                // todo: don't read longer than last known good commit (writer might be working concurrently here)
                while (seqReadStream.Position < seqReadStream.Length)
                {
                    var record = ReadGlobalSequenceRecord(seqReader);

                    var dataFileName = Path.Combine(_dataDirectory, record.AggregateRootId.ToString(), GetFilename(record.LocalSequenceNumber));
                    yield return ReadEvent(dataFileName);
                }
            }
        }

        public long GetNextGlobalSequenceNumber()
        {
            return ReadGlobalSequenceNumber();
        }

        long ReadGlobalSequenceNumber()
        {
            lock (_lock)
            {
                var globalSequenceNumber = 0L;
                if (_commitsReadStream.Length > 0)
                {
                    _commitsReadStream.Seek(-sizeofCommitRecord, SeekOrigin.End);
                    globalSequenceNumber = _commitsReader.ReadInt64();
                }

                MaybeRecoverFromFailure(globalSequenceNumber);

                return globalSequenceNumber;
            }
        }

        void MaybeRecoverFromFailure(long globalSequenceNumber)
        {
            if (globalSequenceNumber == _seqWriteStream.Length / sizeofSeqRecord)
                return;

            using (var seqReadStream = new FileStream(_seqFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 100 * sizeofSeqRecord, FileOptions.None))
            using (var seqReader = new BinaryReader(seqReadStream, Encoding.ASCII))
            {
                seqReadStream.Seek(globalSequenceNumber * sizeofSeqRecord, SeekOrigin.Begin);

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

            _seqWriteStream.SetLength(globalSequenceNumber * sizeofSeqRecord);
            _seqWriteStream.Flush();
        }

        static GlobalSequenceRecord ReadGlobalSequenceRecord(BinaryReader seqReader)
        {
            return new GlobalSequenceRecord
            {
                GlobalSequenceNumber = seqReader.ReadInt64(),
                BatchId = new Guid(seqReader.ReadBytes(16)),
                BatchSize = seqReader.ReadInt32(),
                AggregateRootId = new Guid(seqReader.ReadBytes(16)),
                LocalSequenceNumber = seqReader.ReadInt64(),
                Checksum = seqReader.ReadInt32()
            };
        }

        DomainEvent ReadEvent(string filename)
        {
            using (var filestream = new FileStream(filename, FileMode.Open))
            using (var reader = new BsonReader(filestream))
            {
                return _serializer.Deserialize<DomainEvent>(reader);
            }
        }

        void WriteEvent(string filename, DomainEvent domainEvent)
        {
            using (var eventFileStream = new FileStream(filename, FileMode.CreateNew, FileAccess.Write, FileShare.None, 1024, FileOptions.None))
            using (var eventFileWriter = new BinaryWriter(eventFileStream))
            using (var bsonWriter = new BsonWriter(eventFileWriter))
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
            _seqWriter.Dispose();
            _seqWriteStream.Dispose();
            _commitsWriter.Dispose();
            _commitsWriteStream.Dispose();
            _commitsReader.Dispose();
            _commitsReadStream.Dispose();
        }

        public class GlobalSequenceRecord
        {
            public long GlobalSequenceNumber { get; set; }
            public Guid BatchId { get; set; }
            public int BatchSize { get; set; }
            public Guid AggregateRootId { get; set; }
            public long LocalSequenceNumber { get; set; }
            public int Checksum { get; set; }
        }
    }
}

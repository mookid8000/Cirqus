using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using d60.Cirqus.Events;
using d60.Cirqus.Exceptions;
using d60.Cirqus.Extensions;
using d60.Cirqus.Serialization;

namespace d60.Cirqus.NTFS.Events
{
    public class NtfsEventStore : IEventStore, IDisposable
    {
        const int sizeofLogEntry = 52;

        readonly DomainEventSerializer _serializer = new DomainEventSerializer("<events>");
        readonly string _eventsDirectory;
        readonly BinaryWriter _globalSequenceWriter;
        readonly FileStream _globalSequenceFileStream;
        readonly string _globalSequenceFilePath;

        public NtfsEventStore(string basePath, bool dropEvents = false)
        {
            _eventsDirectory = Path.Combine(basePath, "events");
            _globalSequenceFilePath = Path.Combine(basePath, "global.idx");

            if (dropEvents) DropEvents();

            Directory.CreateDirectory(_eventsDirectory);

            _globalSequenceFileStream = new FileStream(_globalSequenceFilePath, FileMode.Append, FileAccess.Write, FileShare.Read, 1024, FileOptions.None);
            _globalSequenceWriter = new BinaryWriter(_globalSequenceFileStream, Encoding.ASCII);
        }


        public void Save(Guid batchId, IEnumerable<DomainEvent> batch)
        {
            var events = batch.ToList();

            // zero-based sequence based on equal sized entries in the transaction log file
            var globalSequenceNumber = _globalSequenceFileStream.Length/sizeofLogEntry;

            EventValidation.ValidateBatchIntegrity(batchId, events);

            foreach (var domainEvent in events)
            {
                domainEvent.Meta[DomainEvent.MetadataKeys.GlobalSequenceNumber] = globalSequenceNumber++;
                domainEvent.Meta[DomainEvent.MetadataKeys.BatchId] = batchId;

                var aggregateRootId = domainEvent.GetAggregateRootId();
                var sequenceNumber = domainEvent.GetSequenceNumber();

                var aggregateDirectory = Path.Combine(_eventsDirectory, aggregateRootId.ToString());
                Directory.CreateDirectory(aggregateDirectory);

                try
                {
                    _globalSequenceWriter.Write(globalSequenceNumber);
                    _globalSequenceWriter.Write(batchId.ToByteArray());
                    _globalSequenceWriter.Write(aggregateRootId.ToByteArray());
                    _globalSequenceWriter.Write(sequenceNumber);
                    
                    // flush before transaction write so we have a way to find possible orphan events
                    _globalSequenceWriter.Flush(); 

                    var eventPath = Path.Combine(aggregateDirectory, GetFilename(sequenceNumber));
                    using (var eventFileStream = new FileStream(eventPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 1024, FileOptions.None))
                    using (var eventFileWriter = new StreamWriter(eventFileStream, Encoding.UTF8, 1024))
                        eventFileWriter.Write(_serializer.Serialize(domainEvent));

                    // todo write a checksum per event in batch so each entry in index file is same size
                    _globalSequenceWriter.Write(123456);
                    _globalSequenceWriter.Flush();
                }
                catch (IOException e)
                {
                    throw new ConcurrencyException(batchId, events, e);
                }
            }
        }

        public IEnumerable<DomainEvent> Load(Guid aggregateRootId, long firstSeq = 0, long limit = Int32.MaxValue)
        {
            var aggregateDirectory = Path.Combine(_eventsDirectory, aggregateRootId.ToString());
            
            if (!Directory.Exists(aggregateDirectory))
                return Enumerable.Empty<DomainEvent>();

            return from path in Directory.EnumerateFiles(aggregateDirectory)
                   let seq = int.Parse(Path.GetFileName(path))
                   where seq >= firstSeq && seq < firstSeq + limit
                   select _serializer.Deserialize(File.ReadAllText(path));
        }

        public IEnumerable<DomainEvent> Stream(long globalSequenceNumber = 0)
        {
            using (var globalSequenceReadStream = new FileStream(_globalSequenceFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 1024, FileOptions.None))
            using (var reader = new BinaryReader(globalSequenceReadStream, Encoding.ASCII))
            {
                globalSequenceReadStream.Seek(globalSequenceNumber*sizeofLogEntry, SeekOrigin.Begin);

                while (globalSequenceReadStream.Position < globalSequenceReadStream.Length)
                {
                    var readGlobalSequenceNumber = reader.ReadInt64();
                    var batchId = new Guid(reader.ReadBytes(16));
                    var aggregateRootId = new Guid(reader.ReadBytes(16));
                    var sequenceNumber = reader.ReadInt64();
                    var checksum = reader.ReadInt32();

                    yield return _serializer.Deserialize(File.ReadAllText(
                        Path.Combine(_eventsDirectory, aggregateRootId.ToString(), GetFilename(sequenceNumber)),
                        Encoding.UTF8));
                }
            }
        }

        static string GetFilename(long sequenceNumber)
        {
            return sequenceNumber.ToString(CultureInfo.InvariantCulture).PadLeft(20, '0');
        }

        void DropEvents()
        {
            foreach (var file in new DirectoryInfo(_eventsDirectory).GetFiles()) file.Delete();
            foreach (var dir in new DirectoryInfo(_eventsDirectory).GetDirectories()) dir.Delete(true);

            if (File.Exists(_globalSequenceFilePath))
                File.Delete(_globalSequenceFilePath);
        }

        public void Dispose()
        {
            _globalSequenceWriter.Dispose();
            _globalSequenceFileStream.Dispose();
        }
    }
}

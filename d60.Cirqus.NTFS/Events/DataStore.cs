using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using d60.Cirqus.Events;
using d60.Cirqus.Exceptions;
using d60.Cirqus.Extensions;
using d60.Cirqus.Numbers;
using d60.Cirqus.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;

namespace d60.Cirqus.NTFS.Events
{
    internal class DataStore
    {
        readonly JsonSerializer _serializer;
        readonly string _dataDirectory;

        public DataStore(string basePath, bool dropEvents)
        {
            _serializer = CreateSerializer();
            _dataDirectory = Path.Combine(basePath, "events");

            if (dropEvents && Directory.Exists(_dataDirectory))
                new DirectoryInfo(_dataDirectory).Delete(true);

            Directory.CreateDirectory(_dataDirectory);
        }

        public void Write(Guid batchId, IReadOnlyCollection<DomainEvent> events)
        {
            foreach (var domainEvent in events)
            {
                try
                {
                    Write(domainEvent);
                }
                catch (IOException exception)
                {
                    throw new ConcurrencyException(batchId, events, exception);
                }
            }
        }

        public void Write(DomainEvent domainEvent)
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

        public IEnumerable<DomainEvent> Read(long lastCommittedGlobalSequenceNumber, Guid aggregateRootId, long offset, long take)
        {
            var aggregateDirectory = Path.Combine(_dataDirectory, aggregateRootId.ToString());

            if (!Directory.Exists(aggregateDirectory))
                return Enumerable.Empty<DomainEvent>();

            return from path in Directory.EnumerateFiles(aggregateDirectory)
                   let seq = long.Parse(Path.GetFileName(path))
                   where seq >= offset && seq < offset + take
                   let @event = TryRead(path)
                   where @event != null && @event.GetGlobalSequenceNumber(true) <= lastCommittedGlobalSequenceNumber
                   select @event;

        }

        public DomainEvent Read(Guid aggregateRootId, long sequenceNumber)
        {
            var filename = Path.Combine(_dataDirectory, aggregateRootId.ToString(), GetFilename(sequenceNumber));
            
            var @event = TryRead(filename);
            
            if (@event == null)
            {
                throw new InvalidOperationException("The event you tried to read did not exist on disk or was corrupted.");
            }

            return @event;
        }

        public void Truncate(Guid aggregateRootId, long sequenceNumber)
        {
            var filename = Path.Combine(_dataDirectory, aggregateRootId.ToString(), GetFilename(sequenceNumber));
            if (File.Exists(filename))
                File.Delete(filename);
        }

        DomainEvent TryRead(string filename)
        {
            if (!File.Exists(filename))
                return null;

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

        static JsonSerializer CreateSerializer()
        {
            var binder = new TypeAliasBinder("<events>");
            var serializer = new JsonSerializer
            {
                Binder = binder.AddType(typeof(Metadata)),
                TypeNameHandling = TypeNameHandling.Objects,
                Formatting = Formatting.Indented
            };
            return serializer;
        }

        static string GetFilename(long sequenceNumber)
        {
            return sequenceNumber.ToString(CultureInfo.InvariantCulture).PadLeft(20, '0');
        }
    }
}
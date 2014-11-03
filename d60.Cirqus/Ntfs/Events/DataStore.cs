using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using d60.Cirqus.Events;
using d60.Cirqus.Exceptions;
using d60.Cirqus.Extensions;
using d60.Cirqus.Numbers;
using d60.Cirqus.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;

namespace d60.Cirqus.Ntfs.Events
{
    /// <summary>
    /// Stores events as individual files utilizing the file system to do indexed lookup.
    /// Enforces AggregateId/SequenceNumber uniqueness.
    /// Reading and writing can be done concurrently.
    /// </summary>
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

        public void Write(Guid batchId, IReadOnlyCollection<Cirqus.Events.EventData> events)
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

        public void Write(Cirqus.Events.EventData domainEvent)
        {
            var aggregateRootId = domainEvent.GetAggregateRootId();
            var sequenceNumber = domainEvent.GetSequenceNumber();

            var aggregateDirectory = Path.Combine(_dataDirectory, aggregateRootId.ToString());
            Directory.CreateDirectory(aggregateDirectory);

            var filename = Path.Combine(aggregateDirectory, GetFilename(sequenceNumber));

            using (var fileStream = new FileStream(filename, FileMode.CreateNew, FileAccess.Write, FileShare.None, 1024, FileOptions.None))
            using (var bsonWriter = new BsonWriter(fileStream))
            {
                _serializer.Serialize(bsonWriter, EventData.Create(domainEvent));
            }
        }

        class EventData
        {
            public EventData(Metadata meta, byte[] data)
            {
                Meta = meta;
                Data = data;
            }

            public static EventData Create(Cirqus.Events.EventData domainEvent)
            {
                return new EventData(domainEvent.Meta, domainEvent.Data);
            }

            public Metadata Meta { get; private set; }
            public byte[] Data { get; private set; }
        }

        public IEnumerable<Cirqus.Events.EventData> Read(long lastCommittedGlobalSequenceNumber, Guid aggregateRootId, long offset)
        {
            var aggregateDirectory = Path.Combine(_dataDirectory, aggregateRootId.ToString());

            if (!Directory.Exists(aggregateDirectory))
            {
                return Enumerable.Empty<Cirqus.Events.EventData>();
            }

            return from path in Directory.EnumerateFiles(aggregateDirectory)
                   let seq = long.Parse(Path.GetFileName(path))
                   where seq >= offset
                   let @event = TryRead(path)
                   where @event != null && @event.Meta.ContainsKey(DomainEvent.MetadataKeys.GlobalSequenceNumber) &&
                         @event.GetGlobalSequenceNumber(throwIfNotFound: true) <= lastCommittedGlobalSequenceNumber
                   select @event;

        }

        public Cirqus.Events.EventData Read(Guid aggregateRootId, long sequenceNumber)
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

        Cirqus.Events.EventData TryRead(string filename)
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
                    var eventData = _serializer.Deserialize<EventData>(bsonReader);
                    
                    return Cirqus.Events.EventData.FromMetadata(eventData.Meta, eventData.Data);
                }
                catch (Exception)
                {
                    return null;
                }
            }
        }

        static JsonSerializer CreateSerializer()
        {
            return new JsonSerializer
            {
                TypeNameHandling = TypeNameHandling.None,
                Formatting = Formatting.Indented
            };
        }

        static string GetFilename(long sequenceNumber)
        {
            return sequenceNumber.ToString(CultureInfo.InvariantCulture).PadLeft(20, '0');
        }
    }
}
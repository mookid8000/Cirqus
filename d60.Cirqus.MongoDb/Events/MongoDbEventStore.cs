using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using d60.Cirqus.Events;
using d60.Cirqus.Exceptions;
using d60.Cirqus.Extensions;
using d60.Cirqus.Numbers;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using MongoDB.Driver.Builders;

namespace d60.Cirqus.MongoDb.Events
{
    public class MongoDbEventStore : IEventStore
    {
        const string GlobalSeqUniquenessIndexName = "EnsureGlobalSeqUniqueness";
        const string SeqUniquenessIndexName = "EnsureSeqUniqueness";
        const string EventsDocPath = "Events";
        const string MetaDocPath = "Meta";

        static readonly string SeqNoDocPath = string.Format("{0}.{1}.{2}", EventsDocPath, MetaDocPath, DomainEvent.MetadataKeys.SequenceNumber);
        static readonly string GlobalSeqNoDocPath = string.Format("{0}.{1}.{2}", EventsDocPath, MetaDocPath, DomainEvent.MetadataKeys.GlobalSequenceNumber);
        static readonly string AggregateRootIdDocPath = string.Format("{0}.{1}.{2}", EventsDocPath, MetaDocPath, DomainEvent.MetadataKeys.AggregateRootId);

        readonly MongoDbSerializer _serializer = new MongoDbSerializer();

        readonly MongoCollection _eventBatches;
        readonly MongoCollection<MongoEventBatch> _eventBatches2;

        public MongoDbEventStore(MongoDatabase database, string eventCollectionName, bool automaticallyCreateIndexes = true)
        {
            _eventBatches = database.GetCollection(eventCollectionName);
            _eventBatches2 = database.GetCollection<MongoEventBatch>(eventCollectionName);

            if (automaticallyCreateIndexes)
            {
                _eventBatches.CreateIndex(IndexKeys.Ascending(GlobalSeqNoDocPath), IndexOptions.SetUnique(true).SetName(GlobalSeqUniquenessIndexName));
                _eventBatches.CreateIndex(IndexKeys.Ascending(AggregateRootIdDocPath, SeqNoDocPath), IndexOptions.SetUnique(true).SetName(SeqUniquenessIndexName));
            }
        }

        public void AddSerializationMutator(IJsonEventMutator mutator)
        {
            _serializer.EventSerializationMutators.Add(mutator);
        }

        public void AddDeserializationMutator(IJsonEventMutator mutator)
        {
            _serializer.EventDeserializationMutators.Add(mutator);
        }

        public IEnumerable<DomainEvent> Stream(long globalSequenceNumber = 0)
        {
            var globalSequenceNumberToQueryFor = globalSequenceNumber;

            return _eventBatches
                .FindAs<BsonDocument>(Query.GTE(GlobalSeqNoDocPath, globalSequenceNumberToQueryFor))
                .SelectMany(doc => doc[EventsDocPath].AsBsonArray)
                .Select(eventDoc => new
                {
                    GlobalSequenceNumber = GetLong(eventDoc[MetaDocPath][DomainEvent.MetadataKeys.GlobalSequenceNumber]),
                    EventDoc = eventDoc
                })
                .Where(a => a.GlobalSequenceNumber >= globalSequenceNumber)
                .Select(a => _serializer.Deserialize(a.EventDoc));
        }

        public IEnumerable<DomainEvent> Load(Guid aggregateRootId, long firstSeq = 0)
        {
            var criteria = Query.And(
                Query.EQ(AggregateRootIdDocPath, aggregateRootId.ToString()),
                Query.GTE(SeqNoDocPath, firstSeq));

            var docs = _eventBatches.FindAs<BsonDocument>(criteria);

            var eventsSatisfyingCriteria = docs
                .SelectMany(doc => doc[EventsDocPath].AsBsonArray)
                .Select(e => new
                {
                    Event = e,
                    SequenceNumber = GetLong(e[MetaDocPath][DomainEvent.MetadataKeys.SequenceNumber]),
                    AggregateRootId = GetAggregateRootIdOrDefault(e)
                })
                .Where(e => e.AggregateRootId == aggregateRootId && e.SequenceNumber >= firstSeq);

            return eventsSatisfyingCriteria
                .OrderBy(e => e.SequenceNumber)
                .Select(e => _serializer.Deserialize(e.Event));
        }

        public void Save(Guid batchId, IEnumerable<DomainEvent> batch)
        {
            var events = batch.ToList();

            if (!events.Any())
            {
                throw new InvalidOperationException(string.Format("Attempted to save batch {0}, but the batch of events was empty!", batchId));
            }

            events.ForEach(e => _serializer.EnsureSerializability(e));

            var nextGlobalSeqNo = GetNextGlobalSequenceNumber();

            foreach (var e in events)
            {
                e.Meta[DomainEvent.MetadataKeys.GlobalSequenceNumber] = (nextGlobalSeqNo++).ToString(Metadata.NumberCulture);
                e.Meta[DomainEvent.MetadataKeys.BatchId] = batchId.ToString();
            }

            EventValidation.ValidateBatchIntegrity(batchId, events);

            var doc = new BsonDocument
            {
                {"_id", batchId.ToString()},
                {EventsDocPath, GetEvents(events)}
            };

            try
            {
                _eventBatches.Save(doc);
            }
            catch (MongoDuplicateKeyException exception)
            {
                throw new ConcurrencyException(batchId, events, exception);
            }
        }

        public long GetNextGlobalSequenceNumber()
        {
            var doc = _eventBatches
                .FindAllAs<BsonDocument>()
                .SetSortOrder(SortBy.Descending(GlobalSeqNoDocPath))
                .SetLimit(1)
                .SingleOrDefault();

            return doc == null
                ? 0
                : doc[EventsDocPath].AsBsonArray
                    .Select(e => e[MetaDocPath][DomainEvent.MetadataKeys.GlobalSequenceNumber].ToInt64())
                    .Max() + 1;
        }

        public void Save(Guid batchId, IEnumerable<Event> events)
        {
            var batch = events.ToList();

            if (!batch.Any())
            {
                throw new InvalidOperationException(string.Format("Attempted to save batch {0}, but the batch of events was empty!", batchId));
            }

            var nextGlobalSeqNo = GetNextGlobalSequenceNumber();

            foreach (var e in batch)
            {
                e.Meta[DomainEvent.MetadataKeys.GlobalSequenceNumber] = (nextGlobalSeqNo++).ToString(Metadata.NumberCulture);
                e.Meta[DomainEvent.MetadataKeys.BatchId] = batchId.ToString();
            }

            EventValidation.ValidateBatchIntegrity(batchId, batch);

            var doc = new BsonDocument
            {
                {"_id", batchId.ToString()},
                {EventsDocPath, GetEventsNew(batch)}
            };

            try
            {
                //_eventBatches.Save(doc);

            }
            catch (MongoDuplicateKeyException exception)
            {
                throw new ConcurrencyException(batchId, batch, exception);
            }
        }

        BsonValue GetEventsNew(IEnumerable<Event> batch)
        {
            var array = new BsonArray();

            foreach (var e in batch)
            {
                var doc = new BsonDocument();

                doc["Meta"] = Serialize(e.Meta);

                if (e.IsJson())
                {
                    var text = Encoding.UTF8.GetString(e.Data);

                    //var bson = BsonDocument.Parse(text);

                    doc["Body"] = text;
                }
                else
                {
                    doc["Bin"] = BsonValue.Create(e.Data);
                }

                array.Add(doc);
            }

            return array;
        }

        BsonValue Serialize(Metadata meta)
        {
            var doc = new BsonDocument();

            foreach (var kvp in meta)
            {
                if (kvp.Value is Guid)
                {
                    doc[kvp.Key] = kvp.Value.ToString();
                }
                else
                {
                    doc[kvp.Key] = BsonValue.Create(kvp.Value);
                }
            }

            return doc;
        }

        public IEnumerable<Event> LoadNew(Guid aggregateRootId, long firstSeq = 0)
        {
            var criteria = Query.And(
                Query.EQ(AggregateRootIdDocPath, aggregateRootId.ToString()),
                Query.GTE(SeqNoDocPath, firstSeq));

            var docs = _eventBatches.FindAs<BsonDocument>(criteria);

            var eventsSatisfyingCriteria = docs
                .SelectMany(doc => doc[EventsDocPath].AsBsonArray)
                .Select(e => new
                {
                    Event = e,
                    Meta = e["Meta"],
                    SequenceNumber = GetLong(e[MetaDocPath][DomainEvent.MetadataKeys.SequenceNumber]),
                    AggregateRootId = GetAggregateRootIdOrDefault(e)
                })
                .Where(e => e.AggregateRootId == aggregateRootId && e.SequenceNumber >= firstSeq);

            return eventsSatisfyingCriteria
                .OrderBy(e => e.SequenceNumber)
                .Select(e =>
                {
                    var bsonValue = e.Event;
                    var meta = DeserializeMeta(e.Meta);

                    if (bsonValue["Body"] != null)
                    {
                        return new Event
                        {
                            Meta = meta,
                            Data = Encoding.UTF8.GetBytes(bsonValue["Body"].AsString),
                        };
                    }

                    return new Event
                    {
                        Meta = meta,
                        Data = bsonValue["Bin"].AsByteArray
                    };
                });
        }

        public IEnumerable<Event> StreamNew(long globalSequenceNumber = 0)
        {
            return Enumerable.Empty<Event>();
        }

        static Metadata DeserializeMeta(BsonValue bsonValue)
        {
            var meta = new Metadata();
            foreach (var property in bsonValue.AsBsonDocument)
            {
                meta[property.Name] = property.Value.ToString();
            }
            return meta;
        }

        long GetLong(BsonValue bsonValue)
        {
            if (bsonValue.IsInt32)
                return bsonValue.ToInt32();

            if (bsonValue.IsInt64)
                return bsonValue.ToInt64();

            throw new FormatException(string.Format("Could not intepret BSON value '{0}' as int or long - its type is '{1}'",
                bsonValue, bsonValue.BsonType));
        }

        Guid GetAggregateRootIdOrDefault(BsonValue e)
        {
            var metaDoc = e[MetaDocPath].AsBsonDocument;

            return new Guid(metaDoc.GetValue(DomainEvent.MetadataKeys.AggregateRootId, Guid.Empty.ToString()).ToString());
        }

        BsonValue GetEvents(IEnumerable<DomainEvent> events)
        {
            var array = new BsonArray();

            foreach (var e in events)
            {
                array.Add(_serializer.Serialize(e));
            }

            return array;
        }
    }

    class MongoEventBatch
    {
        [BsonId]
        public string BatchId { get; set; }

        public Dictionary<string,string> Meta { get; set; }

        public List<MongoEvent> Events { get; set; }
    }

    class MongoEvent
    {
        public byte[] Bin { get; set; }
        public string Body { get; set; }
    }
}
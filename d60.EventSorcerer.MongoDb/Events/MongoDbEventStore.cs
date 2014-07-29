using System;
using System.Collections.Generic;
using System.Linq;
using d60.EventSorcerer.Events;
using d60.EventSorcerer.Numbers;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using MongoDB.Driver.Builders;

namespace d60.EventSorcerer.MongoDb.Events
{
    public class MongoDbEventStore : IEventStore, ISequenceNumberGenerator
    {
        static readonly string SeqNoDocPath = string.Format("Events.Meta.{0}", DomainEvent.MetadataKeys.SequenceNumber);
        static readonly string AggregateRootIdDocPath = string.Format("Events.Meta.{0}", DomainEvent.MetadataKeys.AggregateRootId);
        
        const string SeqUniquenessIndexName = "EnsureSeqUniqueness";
        const string AggregateRootIndexName = "AggregateRootId";
        const string EventsDocPath = "Events";
        const string MetaDocPath = "Meta";

        readonly MongoDbSerializer _serializer = new MongoDbSerializer();
        readonly MongoCollection _eventBatches;

        public MongoDbEventStore(MongoDatabase database, string eventCollectionName, bool automaticallyCreateIndexes = true)
        {
            _eventBatches = database.GetCollection(eventCollectionName);

            if (automaticallyCreateIndexes)
            {
                _eventBatches.CreateIndex(IndexKeys.Ascending(SeqNoDocPath, AggregateRootIdDocPath), IndexOptions.SetUnique(true).SetName(SeqUniquenessIndexName));
                _eventBatches.CreateIndex(IndexKeys.Ascending(AggregateRootIdDocPath), IndexOptions.SetName(AggregateRootIndexName));
            }
        }

        public int Next(Guid aggregateRootId)
        {
            return GetNextSeq(aggregateRootId);
        }

        public int GetNextSeq(Guid aggregateRootId)
        {
            var doc = _eventBatches.FindAs<BsonDocument>(Query.EQ(AggregateRootIdDocPath, aggregateRootId.ToString()))
                .SetSortOrder(SortBy.Descending(SeqNoDocPath))
                .SetLimit(1)
                .SingleOrDefault();

            return doc == null
                ? 0
                : doc[EventsDocPath].AsBsonArray
                    .Select(e => e[MetaDocPath][DomainEvent.MetadataKeys.SequenceNumber].ToInt32())
                    .Max() + 1;
        }

        public IEnumerable<DomainEvent> Load(Guid aggregateRootId, int firstSeq = 0, int limit = int.MaxValue)
        {
            return InnerLoad(firstSeq, limit, aggregateRootId);
        }

        public IEnumerable<DomainEvent> Load(int firstSeq, int limit)
        {
            return InnerLoad(firstSeq, limit, Guid.Empty);
        }


        public void Save(Guid batchId, IEnumerable<DomainEvent> batch)
        {
            var events = batch.ToList();

            if (!events.Any())
            {
                throw new InvalidOperationException(string.Format("Attempted to save batch {0}, but the batch of events was empty!", batchId));
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
                throw new ConcurrencyException(batchId, batch, exception);
            }
        }

        static Guid GetAggregateRootIdOrDefault(BsonValue e)
        {
            var metaDoc = e[MetaDocPath].AsBsonDocument;

            return new Guid(metaDoc.GetValue(DomainEvent.MetadataKeys.AggregateRootId, Guid.Empty.ToString()).ToString());
        }

        IEnumerable<DomainEvent> InnerLoad(int firstSeq, int limit, Guid aggregateRootId)
        {
            var lastSeq = firstSeq + limit;
            var criteria = Query.And(
                Query.GTE(SeqNoDocPath, firstSeq),
                Query.LT(SeqNoDocPath, lastSeq));

            if (aggregateRootId != Guid.Empty)
            {
                criteria = Query.And(criteria, Query.EQ(AggregateRootIdDocPath, aggregateRootId.ToString()));
            }

            var docs = _eventBatches.FindAs<BsonDocument>(criteria);

            var eventsSatisfyingCriteria = docs
                .SelectMany(doc => doc[EventsDocPath].AsBsonArray)
                .Select(e => new
                {
                    Event = e,
                    SequenceNumber = e[MetaDocPath][DomainEvent.MetadataKeys.SequenceNumber].ToInt32(),
                    AggregateRootId = GetAggregateRootIdOrDefault(e)
                })
                .Where(e => e.SequenceNumber >= firstSeq && e.SequenceNumber < lastSeq);

            if (aggregateRootId != Guid.Empty)
            {
                eventsSatisfyingCriteria = eventsSatisfyingCriteria
                    .Where(e => e.AggregateRootId == aggregateRootId);
            }

            return eventsSatisfyingCriteria
                .OrderBy(e => e.SequenceNumber)
                .Select(e => _serializer.Deserialize(e.Event));
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

    class EventBatch
    {
        [BsonId]
        public string BatchId { get; set; }

        public List<DomainEvent> Events { get; set; }
    }
}
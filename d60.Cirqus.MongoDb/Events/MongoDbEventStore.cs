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

        static readonly string SeqNoDocPath = string.Format("{0}.SequenceNumber", EventsDocPath);
        static readonly string GlobalSeqNoDocPath = string.Format("{0}.GlobalSequenceNumber", EventsDocPath);
        static readonly string AggregateRootIdDocPath = string.Format("{0}.AggregateRootId", EventsDocPath);

        readonly MongoDbSerializer _serializer = new MongoDbSerializer();
        readonly MongoCollection<MongoEventBatch> _eventBatches;

        public MongoDbEventStore(MongoDatabase database, string eventCollectionName, bool automaticallyCreateIndexes = true)
        {
            _eventBatches = database.GetCollection<MongoEventBatch>(eventCollectionName);

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

        public IEnumerable<Event> Stream(long globalSequenceNumber = 0)
        {
            var criteria = Query.GTE(GlobalSeqNoDocPath, globalSequenceNumber);

            return _eventBatches.Find(criteria)
                .SelectMany(b => b.Events)
                .OrderBy(e => e.GlobalSequenceNumber)
                .Where(e => e.GlobalSequenceNumber >= globalSequenceNumber)
                .Select(MongoEventToEvent);
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

            try
            {
                _eventBatches.Save(new MongoEventBatch
                {
                    BatchId = batchId.ToString(),
                    Events = batch
                        .Select(b =>
                        {
                            var isJson = b.IsJson();

                            return new MongoEvent
                            {
                                Meta = GetMetadataAsDictionary(b.Meta),
                                Bin = isJson ? null : b.Data,
                                Body = isJson ? GetBsonValue(b.Data) : null,
                                SequenceNumber = b.GetSequenceNumber(),
                                GlobalSequenceNumber = b.GetGlobalSequenceNumber(),
                                AggregateRootId = b.GetAggregateRootId()
                            };
                        })
                        .ToList()
                });
            }
            catch (MongoDuplicateKeyException exception)
            {
                throw new ConcurrencyException(batchId, batch, exception);
            }
        }

        BsonValue GetBsonValue(byte[] data)
        {
            var json = Encoding.UTF8.GetString(data);
            var doc = BsonDocument.Parse(json);

            ReplacePropertyPrefixes(doc, "$", "¤");

            return doc;
        }

        void ReplacePropertyPrefixes(BsonDocument doc, string prefixToReplace, string replacement)
        {
            foreach (var property in doc.ToList())
            {
                if (property.Name.StartsWith(prefixToReplace))
                {
                    doc.Remove(property.Name);

                    doc.Add(new BsonElement(replacement + property.Name.Substring(1), property.Value));
                }

                if (property.Value.IsBsonDocument)
                {
                    ReplacePropertyPrefixes(property.Value.AsBsonDocument, "$", "¤");
                }
            }
        }

        Dictionary<string, string> GetMetadataAsDictionary(Metadata meta)
        {
            return meta.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }

        Metadata GetDictionaryAsMetadata(Dictionary<string, string> dictionary)
        {
            var metadata = new Metadata();
            foreach (var kvp in dictionary)
            {
                metadata[kvp.Key] = kvp.Value;
            }
            return metadata;
        }

        public IEnumerable<Event> Load(Guid aggregateRootId, long firstSeq = 0)
        {
            var criteria = Query.And(
                Query.EQ(AggregateRootIdDocPath, aggregateRootId),
                Query.GTE(SeqNoDocPath, firstSeq));

            return _eventBatches.Find(criteria)
                .SelectMany(b => b.Events)
                .OrderBy(e => e.GlobalSequenceNumber)
                .Where(e => e.GlobalSequenceNumber >= firstSeq && e.AggregateRootId == aggregateRootId)
                .Select(MongoEventToEvent);
        }

        Event MongoEventToEvent(MongoEvent e)
        {
            var meta = GetDictionaryAsMetadata(e.Meta);
            var data = e.Bin ?? GetBytesFromBsonValue(e.Body);

            return Event.FromMetadata(meta, data);
        }

        byte[] GetBytesFromBsonValue(BsonValue body)
        {
            var doc = body.AsBsonDocument;

            ReplacePropertyPrefixes(doc, "¤", "$");

            return Encoding.UTF8.GetBytes(doc.ToString());
        }
    }

    class MongoEventBatch
    {
        [BsonId]
        public string BatchId { get; set; }

        public List<MongoEvent> Events { get; set; }
    }

    class MongoEvent
    {
        public Dictionary<string, string> Meta { get; set; }
        public byte[] Bin { get; set; }
        public BsonValue Body { get; set; }
        
        public long GlobalSequenceNumber { get; set; }
        public long SequenceNumber { get; set; }
        public Guid AggregateRootId { get; set; }
    }
}
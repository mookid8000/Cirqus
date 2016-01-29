using System;
using System.Collections.Generic;
using System.Linq;
using d60.Cirqus.Aggregates;
using d60.Cirqus.Events;
using d60.Cirqus.Extensions;
using d60.Cirqus.Serialization;
using d60.Cirqus.Snapshotting;
using MongoDB.Driver;
using MongoDB.Driver.Builders;

namespace d60.Cirqus.MongoDb.Snapshotting
{
    class NewSnapshottingAggregateRootRepositoryDecorator : IAggregateRootRepository
    {
        readonly IAggregateRootRepository _aggregateRootRepository;
        readonly IEventStore _eventStore;
        readonly IDomainEventSerializer _domainEventSerializer;
        readonly MongoCollection<NewSnapshot> _snapshots;
        readonly Sturdylizer _sturdylizer = new Sturdylizer();

        public NewSnapshottingAggregateRootRepositoryDecorator(IAggregateRootRepository aggregateRootRepository, IEventStore eventStore, IDomainEventSerializer domainEventSerializer, string collectionName, MongoDatabase database)
        {
            _aggregateRootRepository = aggregateRootRepository;
            _eventStore = eventStore;
            _domainEventSerializer = domainEventSerializer;
            _snapshots = database.GetCollection<NewSnapshot>(collectionName);

            var indexKeys = IndexKeys
                .Ascending("AggregateRootId")
                .Ascending("Version")
                .Descending("ValidFromGlobalSequenceNumber");

            _snapshots.CreateIndex(indexKeys);
        }

        public AggregateRoot Get<TAggregateRoot>(string aggregateRootId, IUnitOfWork unitOfWork, long maxGlobalSequenceNumber = long.MaxValue, bool createIfNotExists = false)
        {
            var snapshotAttribute = GetSnapshotAttribute<TAggregateRoot>();

            if (snapshotAttribute == null)
            {
                return _aggregateRootRepository.Get<TAggregateRoot>(aggregateRootId, unitOfWork, maxGlobalSequenceNumber, createIfNotExists);
            }

            var query = Query.And(
                Query.EQ("AggregateRootId", aggregateRootId),
                Query.EQ("Version", snapshotAttribute.Version),
                Query.LT("ValidFromGlobalSequenceNumber", maxGlobalSequenceNumber));

            var matchingSnapshot = _snapshots
                .Find(query)
                .SetSortOrder(SortBy.Descending("ValidFromGlobalSequenceNumber"))
                .SetLimit(1)
                .FirstOrDefault();

            if (matchingSnapshot != null)
            {
                UpdateTimeOfLastUsage(matchingSnapshot);

                var preparedInstance = PrepareFromSnapshot(matchingSnapshot, maxGlobalSequenceNumber, unitOfWork);
                var sequenceNumberOfPreparedInstance = new AggregateRootInfo(preparedInstance).SequenceNumber;

                OnCommitted(aggregateRootId, unitOfWork, preparedInstance, sequenceNumberOfPreparedInstance, snapshotAttribute);

                return preparedInstance;
            }

            var aggregateRootInstance = _aggregateRootRepository.Get<TAggregateRoot>(aggregateRootId, unitOfWork, maxGlobalSequenceNumber, createIfNotExists);
            var checkedOutSequenceNumber = new AggregateRootInfo(aggregateRootInstance).SequenceNumber;

            SaveSnapshot(aggregateRootId, aggregateRootInstance, checkedOutSequenceNumber, snapshotAttribute, maxGlobalSequenceNumber);

            OnCommitted(aggregateRootId, unitOfWork, aggregateRootInstance, checkedOutSequenceNumber, snapshotAttribute);

            return aggregateRootInstance;
        }

        void UpdateTimeOfLastUsage(NewSnapshot matchingSnapshot)
        {
            var query = Query.EQ("Id", matchingSnapshot.Id);
            var update = Update.Set("LastUsedUtc", DateTime.UtcNow);

            _snapshots.Update(query, update, UpdateFlags.None, WriteConcern.Unacknowledged);
        }

        static EnableSnapshotsAttribute GetSnapshotAttribute<TAggregateRoot>()
        {
            return typeof (TAggregateRoot)
                .GetCustomAttributes(typeof (EnableSnapshotsAttribute), false)
                .Cast<EnableSnapshotsAttribute>()
                .FirstOrDefault();
        }

        void OnCommitted(string aggregateRootId, IUnitOfWork unitOfWork, AggregateRoot aggregateRootInstance, long checkedOutSequenceNumber, EnableSnapshotsAttribute snapshotAttribute)
        {
            unitOfWork.Committed += eventBatch =>
            {
                SaveSnapshot(aggregateRootId, aggregateRootInstance, checkedOutSequenceNumber, snapshotAttribute, eventBatch.Max(e => e.GetGlobalSequenceNumber()));
            };
        }

        void SaveSnapshot(string aggregateRootId, AggregateRoot aggregateRootInstance, long checkedOutSequenceNumber, EnableSnapshotsAttribute snapshotAttribute, long validFromGlobalSequenceNumber)
        {
            var info = new AggregateRootInfo(aggregateRootInstance);
            var currentSequenceNumber = info.SequenceNumber;
            if (currentSequenceNumber == checkedOutSequenceNumber) return;

            var serializedInstance = _sturdylizer.SerializeObject(info.Instance);

            _snapshots.Save(new NewSnapshot
            {
                Id = string.Format("{0}/{1}", aggregateRootId, currentSequenceNumber),
                AggregateRootId = aggregateRootId,
                Data = serializedInstance,
                ValidFromGlobalSequenceNumber = validFromGlobalSequenceNumber,
                Version = snapshotAttribute.Version
            });
        }

        AggregateRoot PrepareFromSnapshot(NewSnapshot matchingSnapshot, long maxGlobalSequenceNumber, IUnitOfWork unitOfWork)
        {
            var instance = _sturdylizer.DeserializeObject(matchingSnapshot.Data);

            if (matchingSnapshot.ValidFromGlobalSequenceNumber < maxGlobalSequenceNumber)
            {
                var info = new AggregateRootInfo(instance);

                foreach (var e in _eventStore.Load(matchingSnapshot.AggregateRootId, info.SequenceNumber + 1))
                {
                    if (e.GetGlobalSequenceNumber() > maxGlobalSequenceNumber) break;
                    var domainEvent = _domainEventSerializer.Deserialize(e);
                    info.Apply(domainEvent, unitOfWork);
                }

                info.SetUnitOfWork(unitOfWork);
            }

            return instance;
        }

        public bool Exists(string aggregateRootId, long maxGlobalSequenceNumber = long.MaxValue)
        {
            return _aggregateRootRepository.Exists(aggregateRootId, maxGlobalSequenceNumber);
        }

        class NewSnapshot
        {
            public NewSnapshot()
            {
                LastUsedUtc = DateTime.UtcNow;
            }
            public string Id { get; set; }
            public string AggregateRootId { get; set; }
            public string Data { get; set; }
            public long ValidFromGlobalSequenceNumber { get; set; }
            public int Version { get; set; }
            public DateTime LastUsedUtc { get; set; }
        }
    }
}
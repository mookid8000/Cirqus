using System;
using System.Linq;
using d60.Cirqus.Aggregates;
using d60.Cirqus.Snapshotting;
using d60.Cirqus.Snapshotting.New;
using MongoDB.Driver;
using MongoDB.Driver.Builders;

namespace d60.Cirqus.MongoDb.Snapshotting
{
    class MongoDbSnapshotStore : ISnapshotStore
    {
        readonly Sturdylizer _sturdylizer = new Sturdylizer();
        readonly MongoCollection<NewSnapshot> _snapshots;

        public MongoDbSnapshotStore(MongoDatabase database, string collectionName)
        {
            _snapshots = database.GetCollection<NewSnapshot>(collectionName);

            var indexKeys = IndexKeys
                .Ascending("AggregateRootId")
                .Ascending("Version")
                .Descending("ValidFromGlobalSequenceNumber");

            _snapshots.CreateIndex(indexKeys);
        }

        public Snapshot TryGetSnapshot<TAggregateRoot>(string aggregateRootId, long maxGlobalSequenceNumber)
        {
            var snapshotAttribute = GetSnapshotAttribute<TAggregateRoot>();

            if (snapshotAttribute == null)
            {
                return null;
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

            if (matchingSnapshot == null)
            {
                return null;
            }

            try
            {
                var instance = _sturdylizer.DeserializeObject(matchingSnapshot.Data);
                UpdateTimeOfLastUsage(matchingSnapshot);
                return new Snapshot(matchingSnapshot.ValidFromGlobalSequenceNumber, matchingSnapshot.AggregateRootId, instance);
            }
            catch(Exception)
            {
                return null;
            }
        }

        public void SaveSnapshot<TAggregateRoot>(string aggregateRootId, AggregateRoot aggregateRootInstance, long checkedOutSequenceNumber, bool instanceIsBasedOnSnapshot, long validFromGlobalSequenceNumber)
        {
            var snapshotAttribute = GetSnapshotAttribute(aggregateRootInstance.GetType());
            var info = new AggregateRootInfo(aggregateRootInstance);
            var currentSequenceNumber = info.SequenceNumber;
            if (instanceIsBasedOnSnapshot && currentSequenceNumber == checkedOutSequenceNumber) return;

            var serializedInstance = _sturdylizer.SerializeObject(info.Instance);

            _snapshots.Insert(new NewSnapshot
            {
                Id = string.Format("{0}/{1}", aggregateRootId, currentSequenceNumber),
                AggregateRootId = aggregateRootId,
                Data = serializedInstance,
                ValidFromGlobalSequenceNumber = validFromGlobalSequenceNumber,
                Version = snapshotAttribute.Version
            }, WriteConcern.Unacknowledged);
        }

        public bool EnabledFor<TAggregateRoot>()
        {
            return GetSnapshotAttribute<TAggregateRoot>() != null;
        }

        void UpdateTimeOfLastUsage(NewSnapshot matchingSnapshot)
        {
            var query = Query.EQ("Id", matchingSnapshot.Id);
            var update = Update.Set("LastUsedUtc", DateTime.UtcNow);

            _snapshots.Update(query, update, UpdateFlags.None, WriteConcern.Unacknowledged);
        }

        static EnableSnapshotsAttribute GetSnapshotAttribute<TAggregateRoot>()
        {
            return GetSnapshotAttribute(typeof(TAggregateRoot));
        }

        static EnableSnapshotsAttribute GetSnapshotAttribute(Type type)
        {
            return type
                .GetCustomAttributes(typeof (EnableSnapshotsAttribute), false)
                .Cast<EnableSnapshotsAttribute>()
                .FirstOrDefault();
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
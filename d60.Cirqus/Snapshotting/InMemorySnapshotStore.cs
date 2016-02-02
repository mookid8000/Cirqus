using System;
using System.Collections.Concurrent;
using d60.Cirqus.Aggregates;
using d60.Cirqus.Snapshotting.New;

namespace d60.Cirqus.Snapshotting
{
    class InMemorySnapshotStore : ISnapshotStore
    {
        readonly ConcurrentDictionary<string, InMemSnapshot> _snapshots = new ConcurrentDictionary<string, InMemSnapshot>();
        readonly Sturdylizer _sturdylizer = new Sturdylizer();

        public void SaveSnapshot<TAggregateRoot>(string aggregateRootId, AggregateRoot aggregateRootInstance, long validFromGlobalSequenceNumber)
        {
            _snapshots[aggregateRootId] = new InMemSnapshot(Clone(aggregateRootInstance), validFromGlobalSequenceNumber);
        }

        public Snapshot LoadSnapshot<TAggregateRoot>(string aggregateRootId, long maxGlobalSequenceNumber)
        {
            InMemSnapshot snapshot;
            if (!_snapshots.TryGetValue(aggregateRootId, out snapshot)) return null;
            if (snapshot.ValidFromGlobalSequenceNumber > maxGlobalSequenceNumber) return null;

            snapshot.UpdateLastUsed();

            return new Snapshot(snapshot.ValidFromGlobalSequenceNumber, Clone(snapshot.Instance));
        }

        AggregateRoot Clone(AggregateRoot aggregateRootInstance)
        {
            return _sturdylizer.DeserializeObject(_sturdylizer.SerializeObject(aggregateRootInstance));
        }

        class InMemSnapshot
        {
            public AggregateRoot Instance { get; private set; }
            public long ValidFromGlobalSequenceNumber { get; private set; }
            public DateTime LastUsed { get; private set; }

            public InMemSnapshot(AggregateRoot instance, long validFromGlobalSequenceNumber)
            {
                Instance = instance;
                ValidFromGlobalSequenceNumber = validFromGlobalSequenceNumber;
                LastUsed = DateTime.UtcNow;
            }

            public void UpdateLastUsed()
            {
                LastUsed = DateTime.UtcNow;
            }

            public TimeSpan ElapsedSinceLastUsage
            {
                get { return DateTime.UtcNow - LastUsed; }
            }
        }
    }
}
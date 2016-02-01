using d60.Cirqus.Aggregates;

namespace d60.Cirqus.Snapshotting.New
{
    public interface ISnapshotStore
    {
        Snapshot TryGetSnapshot<TAggregateRoot>(string aggregateRootId, long maxGlobalSequenceNumber);
        void SaveSnapshot<TAggregateRoot>(string aggregateRootId, AggregateRoot aggregateRootInstance, long checkedOutSequenceNumber, bool instanceIsBasedOnSnapshot, long validFromGlobalSequenceNumber);
        bool EnabledFor<TAggregateRoot>();
    }
}
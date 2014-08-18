using System;
using d60.Cirqus.Aggregates;

namespace d60.Cirqus.Snapshotting
{
    public interface ISnapshotCache
    {
        AggregateRootInfo<TAggregateRoot> GetCloneFromCache<TAggregateRoot>(Guid aggregateRootId, long globalSequenceNumber) where TAggregateRoot : AggregateRoot, new();

        void PutCloneToCache<TAggregateRoot>(AggregateRootInfo<TAggregateRoot> aggregateRootInfo) where TAggregateRoot : AggregateRoot, new();
    }
}
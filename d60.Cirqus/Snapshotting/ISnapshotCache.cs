using System;
using d60.Cirqus.Aggregates;

namespace d60.Cirqus.Snapshotting
{
    public interface ISnapshotCache
    {
        /// <summary>
        /// Will return a clone of the most recent entry in the cache whose global sequence number if below or equal to the requested sequence number.
        /// </summary>
        AggregateRootInfo<TAggregateRoot> GetCloneFromCache<TAggregateRoot>(Guid aggregateRootId, long globalSequenceNumber) where TAggregateRoot : AggregateRoot, new();

        /// <summary>
        /// Saves a clone of the specified aggregate root to the cache
        /// </summary>
        void PutCloneToCache<TAggregateRoot>(AggregateRootInfo<TAggregateRoot> aggregateRootInfo) where TAggregateRoot : AggregateRoot, new();
    }
}
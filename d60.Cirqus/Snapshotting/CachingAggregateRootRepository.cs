using System;
using d60.Cirqus.Aggregates;
using d60.Cirqus.Events;

namespace d60.Cirqus.Snapshotting
{
    public class CachingAggregateRootRepository : IAggregateRootRepository
    {
        readonly IAggregateRootRepository _innerAggregateRootRepository;
        readonly ISnapshotCache _snapshotCache;

        public CachingAggregateRootRepository(IAggregateRootRepository innerAggregateRootRepository, ISnapshotCache snapshotCache)
        {
            _innerAggregateRootRepository = innerAggregateRootRepository;
            _snapshotCache = snapshotCache;
        }

        public AggregateRootInfo<TAggregate> Get<TAggregate>(Guid aggregateRootId, IUnitOfWork unitOfWork, long maxGlobalSequenceNumber = Int64.MaxValue) where TAggregate : AggregateRoot, new()
        {
            return _snapshotCache.GetCloneFromCache<TAggregate>(aggregateRootId, maxGlobalSequenceNumber)
                   ?? _innerAggregateRootRepository.Get<TAggregate>(aggregateRootId, unitOfWork, maxGlobalSequenceNumber);
        }

        public bool Exists<TAggregate>(Guid aggregateRootId, long maxGlobalSequenceNumber = Int64.MaxValue, IUnitOfWork unitOfWork = null) where TAggregate : AggregateRoot
        {
            return _innerAggregateRootRepository.Exists<TAggregate>(aggregateRootId, maxGlobalSequenceNumber, unitOfWork);
        }
    }
}
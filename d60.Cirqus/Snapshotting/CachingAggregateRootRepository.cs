using System;
using System.Diagnostics;
using System.Linq;
using d60.Cirqus.Aggregates;
using d60.Cirqus.Events;
using d60.Cirqus.Extensions;

namespace d60.Cirqus.Snapshotting
{
    public class CachingAggregateRootRepository : IAggregateRootRepository
    {
        readonly IAggregateRootRepository _innerAggregateRootRepository;
        readonly ISnapshotCache _snapshotCache;
        readonly IEventStore _eventStore;

        public CachingAggregateRootRepository(IAggregateRootRepository innerAggregateRootRepository, ISnapshotCache snapshotCache, IEventStore eventStore)
        {
            _innerAggregateRootRepository = innerAggregateRootRepository;
            _snapshotCache = snapshotCache;
            _eventStore = eventStore;
        }

        public AggregateRootInfo<TAggregateRoot> Get<TAggregateRoot>(Guid aggregateRootId, IUnitOfWork unitOfWork, long maxGlobalSequenceNumber = Int64.MaxValue) where TAggregateRoot : AggregateRoot, new()
        {
            var cloneFromCache = PrepareCloneFromCache<TAggregateRoot>(aggregateRootId, maxGlobalSequenceNumber, unitOfWork);

            if (cloneFromCache != null) return cloneFromCache;

            var fromRepository = GetFromInnerRepository<TAggregateRoot>(aggregateRootId, unitOfWork, maxGlobalSequenceNumber);

            if (fromRepository.LastSeqNo > 0)
            {
                _snapshotCache.PutCloneToCache(fromRepository);
            }

            return fromRepository;
        }

        public bool Exists<TAggregateRoot>(Guid aggregateRootId, long maxGlobalSequenceNumber = Int64.MaxValue, IUnitOfWork unitOfWork = null) where TAggregateRoot : AggregateRoot
        {
            return _innerAggregateRootRepository.Exists<TAggregateRoot>(aggregateRootId, maxGlobalSequenceNumber, unitOfWork);
        }

        AggregateRootInfo<TAggregateRoot> PrepareCloneFromCache<TAggregateRoot>(Guid aggregateRootId, long maxGlobalSequenceNumber, IUnitOfWork unitOfWork) where TAggregateRoot : AggregateRoot, new()
        {
            var cloneInfo = _snapshotCache.GetCloneFromCache<TAggregateRoot>(aggregateRootId, maxGlobalSequenceNumber);

            if (cloneInfo == null) return null;

            var lastSeqNo = cloneInfo.LastSeqNo;
            var stopwatch = Stopwatch.StartNew();

            var eventsToApply = _eventStore
                .Load(cloneInfo.AggregateRootId, cloneInfo.LastSeqNo + 1)
                .Where(e => e.GetGlobalSequenceNumber() <= maxGlobalSequenceNumber);

            cloneInfo.Apply(eventsToApply, unitOfWork);

            var timeElapsedFetchingAndApplyingEvents = stopwatch.Elapsed;
            var numberOfEventsApplied = cloneInfo.LastSeqNo - lastSeqNo;

            if (timeElapsedFetchingAndApplyingEvents > TimeSpan.FromSeconds(0.1)
                || numberOfEventsApplied > 2)
            {
                _snapshotCache.PutCloneToCache(cloneInfo);
            }

            return cloneInfo;
        }

        AggregateRootInfo<TAggregateRoot> GetFromInnerRepository<TAggregateRoot>(Guid aggregateRootId, IUnitOfWork unitOfWork, long maxGlobalSequenceNumber) where TAggregateRoot : AggregateRoot, new()
        {
            var aggregateRootInfo = _innerAggregateRootRepository.Get<TAggregateRoot>(aggregateRootId, unitOfWork, maxGlobalSequenceNumber);

            return aggregateRootInfo;
        }
    }
}
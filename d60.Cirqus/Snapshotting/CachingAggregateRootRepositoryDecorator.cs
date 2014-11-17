using System;
using System.Diagnostics;
using System.Linq;
using d60.Cirqus.Aggregates;
using d60.Cirqus.Events;
using d60.Cirqus.Extensions;
using d60.Cirqus.Serialization;

namespace d60.Cirqus.Snapshotting
{
    /// <summary>
    /// Decorator for <see cref="IAggregateRootRepository"/> that uses the supplied <see cref="ISnapshotCache"/> to cache
    /// hydrated aggregate roots, always attempting to retrieve a (possibly partially) hydrated root instance from the
    /// cache when it can.
    /// </summary>
    public class CachingAggregateRootRepositoryDecorator : IAggregateRootRepository
    {
        readonly IAggregateRootRepository _innerAggregateRootRepository;
        readonly ISnapshotCache _snapshotCache;
        readonly IEventStore _eventStore;
        readonly IDomainEventSerializer _domainEventSerializer;

        public CachingAggregateRootRepositoryDecorator(IAggregateRootRepository innerAggregateRootRepository, ISnapshotCache snapshotCache, IEventStore eventStore, IDomainEventSerializer domainEventSerializer)
        {
            _innerAggregateRootRepository = innerAggregateRootRepository;
            _snapshotCache = snapshotCache;
            _eventStore = eventStore;
            _domainEventSerializer = domainEventSerializer;
        }

        public AggregateRoot Get<TAggregateRoot>(string aggregateRootId, IUnitOfWork unitOfWork, long maxGlobalSequenceNumber = long.MaxValue, bool createIfNotExists = false)
        {
            var cloneFromCache = PrepareCloneFromCache(aggregateRootId, maxGlobalSequenceNumber, unitOfWork);

            if (cloneFromCache != null) return cloneFromCache;

            var fromRepository = GetFromInnerRepository<TAggregateRoot>(aggregateRootId, unitOfWork, maxGlobalSequenceNumber, createIfNotExists);

            if (fromRepository.CurrentSequenceNumber > 0)
            {
                _snapshotCache.PutCloneToCache(fromRepository);
            }

            return fromRepository;
        }

        public bool Exists(string aggregateRootId, long maxGlobalSequenceNumber = Int64.MaxValue, IUnitOfWork unitOfWork = null)
        {
            return _innerAggregateRootRepository.Exists(aggregateRootId, maxGlobalSequenceNumber, unitOfWork);
        }

        AggregateRoot PrepareCloneFromCache(string aggregateRootId, long maxGlobalSequenceNumber, IUnitOfWork unitOfWork)
        {
            var cloneInfo = _snapshotCache.GetCloneFromCache(aggregateRootId, maxGlobalSequenceNumber);

            if (cloneInfo == null) return null;

            var lastSeqNo = cloneInfo.CurrentSequenceNumber;
            var stopwatch = Stopwatch.StartNew();

            var eventsToApply = _eventStore
                .Load(cloneInfo.Id, cloneInfo.CurrentSequenceNumber + 1)
                .Where(e => e.GetGlobalSequenceNumber() <= maxGlobalSequenceNumber)
                .Select(e => _domainEventSerializer.Deserialize(e));

            foreach (var e in eventsToApply)
            {
                cloneInfo.ApplyEvent(e, ReplayState.ReplayApply);
            }

            var timeElapsedFetchingAndApplyingEvents = stopwatch.Elapsed;
            var numberOfEventsApplied = cloneInfo.CurrentSequenceNumber - lastSeqNo;

            if (timeElapsedFetchingAndApplyingEvents > TimeSpan.FromSeconds(0.1)
                || numberOfEventsApplied > 10)
            {
                _snapshotCache.PutCloneToCache(cloneInfo);
            }

            return cloneInfo;
        }

        AggregateRoot GetFromInnerRepository<TAggregateRoot>(string aggregateRootId, IUnitOfWork unitOfWork, long maxGlobalSequenceNumber, bool createIfNotExists)
        {
            var aggregateRoot = _innerAggregateRootRepository.Get<TAggregateRoot>(aggregateRootId, unitOfWork, maxGlobalSequenceNumber, createIfNotExists);

            return aggregateRoot;
        }
    }
}
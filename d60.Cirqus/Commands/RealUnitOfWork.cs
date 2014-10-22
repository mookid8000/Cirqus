using System;
using System.Collections.Generic;
using d60.Cirqus.Aggregates;
using d60.Cirqus.Events;
using d60.Cirqus.Extensions;

namespace d60.Cirqus.Commands
{
    public class RealUnitOfWork : IUnitOfWork
    {
        readonly IAggregateRootRepository _aggregateRootRepository;
        
        protected readonly List<DomainEvent> Events = new List<DomainEvent>();
        protected readonly Dictionary<long, Dictionary<Guid, AggregateRoot>> CachedAggregateRoots = new Dictionary<long, Dictionary<Guid, AggregateRoot>>();

        public RealUnitOfWork(IAggregateRootRepository aggregateRootRepository)
        {
            _aggregateRootRepository = aggregateRootRepository;
        }

        public IEnumerable<DomainEvent> EmittedEvents
        {
            get { return Events; }
        }

        public void AddEmittedEvent(DomainEvent e)
        {
            Events.Add(e);
        }

        public void AddToCache<TAggregateRoot>(TAggregateRoot aggregateRoot, long globalSequenceNumberCutoff) where TAggregateRoot : AggregateRoot
        {
            var cacheWithThisVersion = CachedAggregateRoots.GetOrAdd(globalSequenceNumberCutoff, cutoff => new Dictionary<Guid, AggregateRoot>());

            cacheWithThisVersion[aggregateRoot.Id] = aggregateRoot;
        }

        public bool Exists<TAggregateRoot>(Guid aggregateRootId, long globalSequenceNumberCutoff) where TAggregateRoot : AggregateRoot
        {
            return _aggregateRootRepository.Exists<TAggregateRoot>(aggregateRootId, globalSequenceNumberCutoff);
        }

        public AggregateRootInfo<TAggregateRoot> Get<TAggregateRoot>(Guid aggregateRootId, long globalSequenceNumberCutoff, bool createIfNotExists = false) where TAggregateRoot : AggregateRoot, new()
        {
            var aggregateRootInfoFromCache = GetAggregateRootFromCache<TAggregateRoot>(aggregateRootId, globalSequenceNumberCutoff);

            if (aggregateRootInfoFromCache != null)
            {
                return aggregateRootInfoFromCache;
            }

            var aggregateRootInfo = _aggregateRootRepository.Get<TAggregateRoot>(aggregateRootId, this, globalSequenceNumberCutoff, createIfNotExists: createIfNotExists);

            // make sure to cache under long.MaxValue if we're "unbounded"
            var lastGlobalSeqNoToCacheUnder = globalSequenceNumberCutoff == long.MaxValue
                ? long.MaxValue
                : aggregateRootInfo.LastGlobalSeqNo;

            AddToCache(aggregateRootInfo.AggregateRoot, lastGlobalSeqNoToCacheUnder);

            return aggregateRootInfo;
        }

        AggregateRootInfo<TAggregateRoot> GetAggregateRootFromCache<TAggregateRoot>(Guid aggregateRootId, long globalSequenceNumberCutoff) where TAggregateRoot : AggregateRoot
        {
            if (!CachedAggregateRoots.ContainsKey(globalSequenceNumberCutoff)) return null;
            if (!CachedAggregateRoots[globalSequenceNumberCutoff].ContainsKey(aggregateRootId)) return null;

            var aggregateRoot = CachedAggregateRoots[globalSequenceNumberCutoff][aggregateRootId];

            if (!(aggregateRoot is TAggregateRoot))
            {
                throw new InvalidOperationException(
                    string.Format("Attempted to load {0} with ID {1} as if it was a {2} - did you use the wrong ID?",
                        aggregateRoot.GetType(), aggregateRootId, typeof (TAggregateRoot)));
            }

            return AggregateRootInfo<TAggregateRoot>.Create((TAggregateRoot)aggregateRoot);
        }
    }
}
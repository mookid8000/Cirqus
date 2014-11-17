using System.Collections.Generic;
using d60.Cirqus.Aggregates;
using d60.Cirqus.Config;
using d60.Cirqus.Events;
using d60.Cirqus.Extensions;

namespace d60.Cirqus.Commands
{
    /// <summary>
    /// Unit of work implementation that works and uses the given <see cref="IAggregateRootRepository"/> to supply aggregate root instances
    /// when it cannot find them in its cache
    /// </summary>
    public class RealUnitOfWork : IUnitOfWork
    {
        readonly IAggregateRootRepository _aggregateRootRepository;
        readonly IDomainTypeNameMapper _typeNameMapper;

        protected readonly List<DomainEvent> Events = new List<DomainEvent>();
        protected readonly Dictionary<long, Dictionary<string, AggregateRoot>> CachedAggregateRoots = new Dictionary<long, Dictionary<string, AggregateRoot>>();

        public RealUnitOfWork(IAggregateRootRepository aggregateRootRepository, IDomainTypeNameMapper typeNameMapper)
        {
            _aggregateRootRepository = aggregateRootRepository;
            _typeNameMapper = typeNameMapper;
        }

        public IEnumerable<DomainEvent> EmittedEvents
        {
            get { return Events; }
        }

        public void AddEmittedEvent<TAggregateRoot>(DomainEvent<TAggregateRoot> e) where TAggregateRoot : AggregateRoot
        {
            e.Meta[DomainEvent.MetadataKeys.Owner] = _typeNameMapper.GetName(typeof (TAggregateRoot));
            e.Meta[DomainEvent.MetadataKeys.Type] = _typeNameMapper.GetName(e.GetType());

            Events.Add(e);
        }

        public void AddToCache(AggregateRoot aggregateRoot, long globalSequenceNumberCutoff)
        {
            var cacheWithThisVersion = CachedAggregateRoots.GetOrAdd(globalSequenceNumberCutoff, cutoff => new Dictionary<string, AggregateRoot>());

            cacheWithThisVersion[aggregateRoot.Id] = aggregateRoot;
        }

        public bool Exists(string aggregateRootId, long globalSequenceNumberCutoff)
        {
            return _aggregateRootRepository.Exists(aggregateRootId, globalSequenceNumberCutoff);
        }

        public AggregateRoot Get(string aggregateRootId, long globalSequenceNumberCutoff, bool createIfNotExists = false)
        {
            var aggregateRootInfoFromCache = GetAggregateRootFromCache(aggregateRootId, globalSequenceNumberCutoff);

            if (aggregateRootInfoFromCache != null)
            {
                return aggregateRootInfoFromCache;
            }

            var aggregateRootInfo = _aggregateRootRepository.Get<AggregateRoot>(aggregateRootId, this, globalSequenceNumberCutoff, createIfNotExists: createIfNotExists);

            // make sure to cache under long.MaxValue if we're "unbounded"
            var lastGlobalSeqNoToCacheUnder = globalSequenceNumberCutoff == long.MaxValue
                ? long.MaxValue
                : aggregateRootInfo.GlobalSequenceNumberCutoff;

            AddToCache(aggregateRootInfo, lastGlobalSeqNoToCacheUnder);

            return aggregateRootInfo;
        }

        AggregateRoot GetAggregateRootFromCache(string aggregateRootId, long globalSequenceNumberCutoff)
        {
            if (!CachedAggregateRoots.ContainsKey(globalSequenceNumberCutoff)) return null;
            if (!CachedAggregateRoots[globalSequenceNumberCutoff].ContainsKey(aggregateRootId)) return null;

            var aggregateRoot = CachedAggregateRoots[globalSequenceNumberCutoff][aggregateRootId];

            return aggregateRoot;

            //if (!(aggregateRoot is TAggregateRoot))
            //{
            //    throw new InvalidOperationException(
            //        string.Format("Attempted to load {0} with ID {1} as if it was a {2} - did you use the wrong ID?",
            //            aggregateRoot.GetType(), aggregateRootId, typeof(TAggregateRoot)));
            //}

            //return AggregateRootInfo<TAggregateRoot>.Create((TAggregateRoot)aggregateRoot);
        }
    }
}
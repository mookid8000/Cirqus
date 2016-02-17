using System;
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

        public void AddEmittedEvent<TAggregateRoot>(AggregateRoot aggregateRoot, DomainEvent<TAggregateRoot> e) where TAggregateRoot : AggregateRoot
        {
            AddEmittedEvent(aggregateRoot.GetType(), e);
        }

        public void AddEmittedEvent(Type aggregateRootType, DomainEvent e)
        {
            e.Meta[DomainEvent.MetadataKeys.Owner] = _typeNameMapper.GetName(aggregateRootType);
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

        public AggregateRoot Get<TAggregateRoot>(string aggregateRootId, long globalSequenceNumberCutoff, bool createIfNotExists)
        {
            var aggregateRootInfoFromCache = GetAggregateRootFromCache(aggregateRootId, globalSequenceNumberCutoff);

            if (aggregateRootInfoFromCache != null)
            {
                return aggregateRootInfoFromCache;
            }

            var aggregateRoot = _aggregateRootRepository.Get<TAggregateRoot>(aggregateRootId, this, globalSequenceNumberCutoff, createIfNotExists: createIfNotExists);

            AddToCache(aggregateRoot, globalSequenceNumberCutoff);

            return aggregateRoot;
        }

        public event Action<IEnumerable<DomainEvent>>  Committed;

        AggregateRoot GetAggregateRootFromCache(string aggregateRootId, long globalSequenceNumberCutoff)
        {
            if (!CachedAggregateRoots.ContainsKey(globalSequenceNumberCutoff)) return null;

            var cachedEntriesForThisInstant = CachedAggregateRoots[globalSequenceNumberCutoff];

            if (!cachedEntriesForThisInstant.ContainsKey(aggregateRootId)) return null;

            var aggregateRoot = cachedEntriesForThisInstant[aggregateRootId];

            return aggregateRoot;
        }

        public void RaiseCommitted(IEnumerable<DomainEvent> eventsFromThisUnitOfWork)
        {
            var committed = Committed;

            if (committed != null)
            {
                committed(eventsFromThisUnitOfWork);
            }
        }
    }
}
using System;
using System.Collections.Generic;
using d60.Cirqus.Aggregates;
using d60.Cirqus.Events;

namespace d60.Cirqus.Tests.Stubs
{
    public class ConsoleOutUnitOfWork : IUnitOfWork
    {
        readonly IAggregateRootRepository _aggregateRootRepository;
        readonly Dictionary<string, AggregateRoot> _cachedAggregateRoots = new Dictionary<string, AggregateRoot>();

        public ConsoleOutUnitOfWork(IAggregateRootRepository aggregateRootRepository)
        {
            _aggregateRootRepository = aggregateRootRepository;
        }

        public void AddEmittedEvent(DomainEvent e)
        {
            Console.WriteLine("Emitted: {0}", e);
        }

        public void AddToCache<TAggregateRoot>(TAggregateRoot aggregateRoot, long globalSequenceNumberCutoff) 
            where TAggregateRoot : AggregateRoot
        {
            _cachedAggregateRoots[aggregateRoot.Id] = aggregateRoot;
        }

        public bool Exists<TAggregateRoot>(string aggregateRootId, long globalSequenceNumberCutoff) 
            where TAggregateRoot : AggregateRoot
        {
            return _aggregateRootRepository.Exists<TAggregateRoot>(aggregateRootId, globalSequenceNumberCutoff);
        }

        public AggregateRootInfo<TAggregateRoot> Get<TAggregateRoot>(string aggregateRootId, long globalSequenceNumberCutoff, bool createIfNotExists) 
            where TAggregateRoot : AggregateRoot, new()
        {
            return _aggregateRootRepository.Get<TAggregateRoot>(aggregateRootId, this, globalSequenceNumberCutoff);
        }

        TAggregateRoot GetAggregateRootFromCache<TAggregateRoot>(string aggregateRootId, long globalSequenceNumberCutoff) 
            where TAggregateRoot : AggregateRoot
        {
            if (!_cachedAggregateRoots.ContainsKey(aggregateRootId)) return null;

            var aggregateRoot = _cachedAggregateRoots[aggregateRootId];

            if (!(aggregateRoot is TAggregateRoot))
            {
                throw new InvalidOperationException(
                    string.Format("Attempted to load {0} with ID {1} as if it was a {2} - did you use the wrong ID?",
                        aggregateRoot.GetType(), aggregateRootId, typeof(TAggregateRoot)));
            }

            return (TAggregateRoot)aggregateRoot;
        }
    }
}
using System;
using System.Collections;
using System.Collections.Generic;
using d60.EventSorcerer.Aggregates;
using d60.EventSorcerer.Events;

namespace d60.EventSorcerer.TestHelpers.Internals
{
    class InMemoryUnitOfWork : IUnitOfWork, IEnumerable<DomainEvent>
    {
        readonly List<DomainEvent> _emittedEvents = new List<DomainEvent>();
        readonly Dictionary<Guid, AggregateRoot> _cachedAggregateRoots = new Dictionary<Guid, AggregateRoot>();
        
        public void AddEmittedEvent(DomainEvent e)
        {
            _emittedEvents.Add(e);
        }

        public TAggregateRoot GetAggregateRootFromCache<TAggregateRoot>(Guid aggregateRootId) where TAggregateRoot : AggregateRoot
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

        public void AddToCache<TAggregateRoot>(TAggregateRoot aggregateRoot) where TAggregateRoot : AggregateRoot
        {
            _cachedAggregateRoots[aggregateRoot.Id] = aggregateRoot;
        }

        public void Clear()
        {
            _emittedEvents.Clear();
            _cachedAggregateRoots.Clear();
        }

        public IEnumerator<DomainEvent> GetEnumerator()
        {
            return _emittedEvents.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
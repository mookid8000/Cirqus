using System;
using System.Collections.Generic;
using d60.Circus.Aggregates;
using d60.Circus.Events;

namespace d60.Circus.Tests.Stubs
{
    public class ConsoleOutUnitOfWork : IUnitOfWork
    {
        readonly Dictionary<Guid, AggregateRoot> _cachedAggregateRoots = new Dictionary<Guid, AggregateRoot>();

        public void AddEmittedEvent(DomainEvent e)
        {
            Console.WriteLine("Emitted: {0}", e);
        }

        public TAggregateRoot GetAggregateRootFromCache<TAggregateRoot>(Guid aggregateRootId, long globalSequenceNumberCutoff) where TAggregateRoot : AggregateRoot
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

        public void AddToCache<TAggregateRoot>(TAggregateRoot aggregateRoot, long globalSequenceNumberCutoff) where TAggregateRoot : AggregateRoot
        {
            _cachedAggregateRoots[aggregateRoot.Id] = aggregateRoot;
        }
    }
}
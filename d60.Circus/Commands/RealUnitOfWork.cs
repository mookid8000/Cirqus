using System;
using System.Collections.Generic;
using d60.Circus.Aggregates;
using d60.Circus.Events;
using d60.Circus.Extensions;

namespace d60.Circus.Commands
{
    public class RealUnitOfWork : IUnitOfWork
    {
        protected readonly List<DomainEvent> Events = new List<DomainEvent>();
        protected readonly Dictionary<long, Dictionary<Guid, AggregateRoot>> CachedAggregateRoots = new Dictionary<long, Dictionary<Guid, AggregateRoot>>();

        public IEnumerable<DomainEvent> EmittedEvents
        {
            get { return Events; }
        }

        public void AddEmittedEvent(DomainEvent e)
        {
            Events.Add(e);
        }

        public TAggregateRoot GetAggregateRootFromCache<TAggregateRoot>(Guid aggregateRootId, long globalSequenceNumberCutoff) where TAggregateRoot : AggregateRoot
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

            return (TAggregateRoot) aggregateRoot;
        }

        public void AddToCache<TAggregateRoot>(TAggregateRoot aggregateRoot, long globalSequenceNumberCutoff) where TAggregateRoot : AggregateRoot
        {
            Console.WriteLine("Adding {0} v {1}", aggregateRoot, globalSequenceNumberCutoff);

            var cacheWithThisVersion = CachedAggregateRoots.GetOrAdd(globalSequenceNumberCutoff, cutoff => new Dictionary<Guid, AggregateRoot>());

            cacheWithThisVersion[aggregateRoot.Id] = aggregateRoot;
        }
    }
}
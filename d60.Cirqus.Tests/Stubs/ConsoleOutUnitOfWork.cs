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

        public void AddEmittedEvent<TAggregateRoot>(AggregateRoot aggregateRoot, DomainEvent<TAggregateRoot> e) where TAggregateRoot : AggregateRoot
        {
            Console.WriteLine("Emitted: {0}", e);
        }

        public void AddToCache(AggregateRoot aggregateRoot, long globalSequenceNumberCutoff)
        {
            _cachedAggregateRoots[aggregateRoot.Id] = aggregateRoot;
        }

        public bool Exists(string aggregateRootId, long globalSequenceNumberCutoff)
        {
            return _aggregateRootRepository.Exists(aggregateRootId, globalSequenceNumberCutoff);
        }

        public AggregateRoot Get<TAggregateRoot>(string aggregateRootId, long globalSequenceNumberCutoff, bool createIfNotExists)
        {
            return _aggregateRootRepository.Get<TAggregateRoot>(aggregateRootId, this, globalSequenceNumberCutoff);
        }

        public event Action Committed;
    }
}
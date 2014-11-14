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

        public void AddEmittedEvent<TAggregateRoot>(DomainEvent<TAggregateRoot> e) where TAggregateRoot : AggregateRoot
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
    }
}
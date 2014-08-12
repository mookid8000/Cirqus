using System;
using d60.Circus.Aggregates;

namespace d60.Circus.Events
{
    /// <summary>
    /// A thing that is capable of collecting emitted events
    /// </summary>
    public interface IUnitOfWork
    {
        void AddEmittedEvent(DomainEvent e);

        TAggregateRoot GetAggregateRootFromCache<TAggregateRoot>(Guid aggregateRootId, long globalSequenceNumberCutoff) where TAggregateRoot : AggregateRoot;

        void AddToCache<TAggregateRoot>(TAggregateRoot aggregateRoot, long globalSequenceNumberCutoff) where TAggregateRoot : AggregateRoot;
    }
}
using System;
using d60.EventSorcerer.Aggregates;

namespace d60.EventSorcerer.Events
{
    /// <summary>
    /// A thing that is capable of collecting emitted events
    /// </summary>
    public interface IUnitOfWork
    {
        void AddEmittedEvent(DomainEvent e);

        TAggregateRoot GetAggregateRootFromCache<TAggregateRoot>(Guid aggregateRootId) where TAggregateRoot : AggregateRoot;

        void AddToCache<TAggregateRoot>(TAggregateRoot aggregateRoot) where TAggregateRoot : AggregateRoot;
    }
}
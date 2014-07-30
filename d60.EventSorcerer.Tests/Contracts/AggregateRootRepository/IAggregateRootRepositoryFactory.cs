using d60.EventSorcerer.Aggregates;
using d60.EventSorcerer.Events;

namespace d60.EventSorcerer.Tests.Contracts.AggregateRootRepository
{
    public interface IAggregateRootRepositoryFactory
    {
        IAggregateRootRepository GetRepo();

        void SaveEvent<TDomainEvent, TAggregateRoot>(TDomainEvent e)
            where TAggregateRoot : AggregateRoot, new()
            where TDomainEvent : DomainEvent<TAggregateRoot>;
    }
}
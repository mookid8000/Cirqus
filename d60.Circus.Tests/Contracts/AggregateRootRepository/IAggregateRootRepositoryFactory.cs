using d60.Circus.Aggregates;
using d60.Circus.Events;

namespace d60.Circus.Tests.Contracts.AggregateRootRepository
{
    public interface IAggregateRootRepositoryFactory
    {
        IAggregateRootRepository GetRepo();

        void SaveEvent<TDomainEvent, TAggregateRoot>(TDomainEvent e)
            where TAggregateRoot : AggregateRoot, new()
            where TDomainEvent : DomainEvent<TAggregateRoot>;
    }
}
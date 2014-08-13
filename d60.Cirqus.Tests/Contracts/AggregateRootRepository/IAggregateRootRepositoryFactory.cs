using d60.Cirqus.Aggregates;
using d60.Cirqus.Events;

namespace d60.Cirqus.Tests.Contracts.AggregateRootRepository
{
    public interface IAggregateRootRepositoryFactory
    {
        IAggregateRootRepository GetRepo();

        void SaveEvent<TDomainEvent, TAggregateRoot>(TDomainEvent e)
            where TAggregateRoot : AggregateRoot, new()
            where TDomainEvent : DomainEvent<TAggregateRoot>;
    }
}
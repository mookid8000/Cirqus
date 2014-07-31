using System;
using d60.EventSorcerer.Aggregates;
using d60.EventSorcerer.Events;
using d60.EventSorcerer.Extensions;
using d60.EventSorcerer.TestHelpers;

namespace d60.EventSorcerer.Tests.Contracts.AggregateRootRepository.Factories
{
    public class InMemoryAggregateRootRepositoryFactory : IAggregateRootRepositoryFactory
    {
        readonly InMemoryAggregateRootRepository _aggregateRootRepository;

        public InMemoryAggregateRootRepositoryFactory()
        {
            _aggregateRootRepository = new InMemoryAggregateRootRepository();
        }

        public IAggregateRootRepository GetRepo()
        {
            return _aggregateRootRepository;
        }

        public void SaveEvent<TDomainEvent, TAggregateRoot>(TDomainEvent e)
            where TAggregateRoot : AggregateRoot, new()
            where TDomainEvent : DomainEvent<TAggregateRoot>
        {
            var rootInstance = _aggregateRootRepository.Get<TAggregateRoot>(e.GetAggregateRootId());

            var handler = rootInstance as IEmit<TDomainEvent>;

            if (handler == null)
            {
                throw new InvalidOperationException(
                    string.Format(
                        "Cannot save event {0} for aggregate root {1} because the root does not implement IEmit<{2}>",
                        e, rootInstance, typeof (TDomainEvent).Name));
            }

            handler.Apply(e);
        }
    }
}
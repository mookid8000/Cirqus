using System;
using d60.EventSorcerer.Aggregates;
using d60.EventSorcerer.Events;
using d60.EventSorcerer.MongoDb.Events;
using d60.EventSorcerer.Tests.MongoDb;

namespace d60.EventSorcerer.Tests.Contracts.AggregateRootRepository.Factories
{
    public class BasicAggregateRootRepositoryFactory : IAggregateRootRepositoryFactory
    {
        readonly MongoDbEventStore _eventStore;
        readonly BasicAggregateRootRepository _basicAggregateRootRepository;

        public BasicAggregateRootRepositoryFactory()
        {
            _eventStore = new MongoDbEventStore(Helper.InitializeTestDatabase(), "events");
            _basicAggregateRootRepository = new BasicAggregateRootRepository(_eventStore);
        }

        public IAggregateRootRepository GetRepo()
        {
            return _basicAggregateRootRepository;
        }

        public void SaveEvent<TDomainEvent, TAggregateRoot>(TDomainEvent e)
            where TAggregateRoot : AggregateRoot, new()
            where TDomainEvent : DomainEvent<TAggregateRoot>
        {
            _eventStore.Save(Guid.NewGuid(), new[] { e });
        }
    }
}
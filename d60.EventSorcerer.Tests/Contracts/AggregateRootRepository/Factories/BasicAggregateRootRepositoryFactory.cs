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
        readonly DefaultAggregateRootRepository _defaultAggregateRootRepository;

        public BasicAggregateRootRepositoryFactory()
        {
            _eventStore = new MongoDbEventStore(MongoHelper.InitializeTestDatabase(), "events");
            _defaultAggregateRootRepository = new DefaultAggregateRootRepository(_eventStore);
        }

        public IAggregateRootRepository GetRepo()
        {
            return _defaultAggregateRootRepository;
        }

        public void SaveEvent<TDomainEvent, TAggregateRoot>(TDomainEvent e)
            where TAggregateRoot : AggregateRoot, new()
            where TDomainEvent : DomainEvent<TAggregateRoot>
        {
            _eventStore.Save(Guid.NewGuid(), new[] { e });
        }
    }
}
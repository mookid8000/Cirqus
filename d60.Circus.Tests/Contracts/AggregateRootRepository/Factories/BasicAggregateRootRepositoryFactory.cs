using System;
using d60.Circus.Aggregates;
using d60.Circus.Events;
using d60.Circus.MongoDb.Events;
using d60.Circus.Tests.MongoDb;

namespace d60.Circus.Tests.Contracts.AggregateRootRepository.Factories
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
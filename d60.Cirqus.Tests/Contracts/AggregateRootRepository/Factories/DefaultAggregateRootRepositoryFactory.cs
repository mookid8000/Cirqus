using System;
using d60.Cirqus.Aggregates;
using d60.Cirqus.Events;
using d60.Cirqus.MongoDb.Events;
using d60.Cirqus.Serialization;
using d60.Cirqus.Tests.MongoDb;

namespace d60.Cirqus.Tests.Contracts.AggregateRootRepository.Factories
{
    public class DefaultAggregateRootRepositoryFactory : IAggregateRootRepositoryFactory
    {
        readonly MongoDbEventStore _eventStore;
        readonly DefaultAggregateRootRepository _defaultAggregateRootRepository;
        readonly DomainEventSerializer _domainEventSerializer = new DomainEventSerializer();

        public DefaultAggregateRootRepositoryFactory()
        {
            _eventStore = new MongoDbEventStore(MongoHelper.InitializeTestDatabase(), "events");
            _defaultAggregateRootRepository = new DefaultAggregateRootRepository(_eventStore, _domainEventSerializer);
        }

        public IAggregateRootRepository GetRepo()
        {
            return _defaultAggregateRootRepository;
        }

        public void SaveEvent<TDomainEvent, TAggregateRoot>(TDomainEvent e)
            where TAggregateRoot : AggregateRoot, new()
            where TDomainEvent : DomainEvent<TAggregateRoot>
        {
            _eventStore.Save(Guid.NewGuid(), new[] { _domainEventSerializer.DoSerialize(e) });
        }
    }
}
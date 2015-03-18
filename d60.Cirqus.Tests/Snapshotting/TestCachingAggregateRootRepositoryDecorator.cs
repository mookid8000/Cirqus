using System;
using System.Collections.Generic;
using System.Linq;
using d60.Cirqus.Aggregates;
using d60.Cirqus.Commands;
using d60.Cirqus.Config;
using d60.Cirqus.Events;
using d60.Cirqus.Extensions;
using d60.Cirqus.MongoDb.Events;
using d60.Cirqus.Serialization;
using d60.Cirqus.Snapshotting;
using d60.Cirqus.Tests.MongoDb;
using NUnit.Framework;

namespace d60.Cirqus.Tests.Snapshotting
{
    [TestFixture]
    public class TestCachingAggregateRootRepositoryDecorator : FixtureBase
    {
        CachingAggregateRootRepositoryDecorator _cachingAggregateRootRepository;
        MongoDbEventStore _eventStore;
        JsonDomainEventSerializer _domainEventSerializer;
        DefaultAggregateRootRepository _realAggregateRootRepository;
        DefaultDomainTypeNameMapper _domainTypeNameMapper;

        protected override void DoSetUp()
        {
            _eventStore = new MongoDbEventStore(MongoHelper.InitializeTestDatabase(), "Events");
            _domainEventSerializer = new JsonDomainEventSerializer();

            _domainTypeNameMapper = new DefaultDomainTypeNameMapper();
            
            var snapshotCache = new InMemorySnapshotCache();

            _realAggregateRootRepository = new DefaultAggregateRootRepository(_eventStore, _domainEventSerializer, _domainTypeNameMapper);
            _cachingAggregateRootRepository = new CachingAggregateRootRepositoryDecorator(_realAggregateRootRepository, snapshotCache, _eventStore, _domainEventSerializer);
        }

        [TestCase(1000, true)]
        [TestCase(1000, false)]
        public void CheckHydrationPerformance(int historyLength, bool useCaching)
        {
            var aggregateRootId = Guid.NewGuid();

            GeneratePrettyLongHistory(aggregateRootId, historyLength);

            TakeTime("Load instance", () =>
            {
                100.Times(() =>
                {
                    var realUnitOfWork = new RealUnitOfWork(_cachingAggregateRootRepository, _domainTypeNameMapper);

                    if (useCaching)
                    {
                        _cachingAggregateRootRepository.Get<Root>(aggregateRootId.ToString(), realUnitOfWork);
                    }
                    else
                    {
                        _realAggregateRootRepository.Get<Root>(aggregateRootId.ToString(), realUnitOfWork);
                    }
                });
            });
        }

        void GeneratePrettyLongHistory(Guid aggregateRootId, int howLong)
        {
            var eventsToSave = Enumerable.Range(0, howLong)
                .Select(i => CreateEvent(aggregateRootId, i))
                .Select(e => _domainEventSerializer.Serialize(e))
                .ToList();

            var random = new Random();

            foreach (var partition in eventsToSave.Batch(100))
            {
                var batchSize = random.Next(10)+1;

                foreach (var batch in partition.Batch(batchSize))
                {
                    _eventStore.Save(Guid.NewGuid(), batch);
                }
            }
        }

        DomainEvent CreateEvent(Guid aggregateRootId, int sequenceNumber)
        {
            return new Event
            {
                Meta =
                {
                    {DomainEvent.MetadataKeys.AggregateRootId, aggregateRootId.ToString()},
                    {DomainEvent.MetadataKeys.SequenceNumber, sequenceNumber.ToString()},
                    {DomainEvent.MetadataKeys.GlobalSequenceNumber, sequenceNumber.ToString()},
                    {DomainEvent.MetadataKeys.Owner, _domainTypeNameMapper.GetName(typeof(Root))},
                    {DomainEvent.MetadataKeys.Type, _domainTypeNameMapper.GetName(typeof(Event))},
                }
            };
        }

        class Root : AggregateRoot, IEmit<Event>
        {
            public int NumberOfProcessedEvents { get; private set; }

            public void Apply(Event e)
            {
                NumberOfProcessedEvents++;
            }
        }

        class Event : DomainEvent<Root> { }
    }
}
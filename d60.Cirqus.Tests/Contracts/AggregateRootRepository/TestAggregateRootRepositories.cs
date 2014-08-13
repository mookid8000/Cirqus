using System;
using d60.Cirqus.Aggregates;
using d60.Cirqus.Events;
using d60.Cirqus.TestHelpers.Internals;
using d60.Cirqus.Tests.Contracts.AggregateRootRepository.Factories;
using NUnit.Framework;

namespace d60.Cirqus.Tests.Contracts.AggregateRootRepository
{
    [TestFixture(typeof(BasicAggregateRootRepositoryFactory))]
    public class TestAggregateRootRepositories<TAggregateRootRepositoryFactory> : FixtureBase where TAggregateRootRepositoryFactory : IAggregateRootRepositoryFactory, new()
    {
        TAggregateRootRepositoryFactory _factory;
        IAggregateRootRepository _repo;

        protected override void DoSetUp()
        {
            _factory = new TAggregateRootRepositoryFactory();
            _repo = _factory.GetRepo();
        }

        [Test]
        public void CanLoadAggregateRoot()
        {
            var aggregateRootId = Guid.NewGuid();

            _factory.SaveEvent<SomeEvent, SomeRoot>(NewEvent(aggregateRootId, 1));
            _factory.SaveEvent<SomeEvent, SomeRoot>(NewEvent(aggregateRootId, 2));
            _factory.SaveEvent<SomeEvent, SomeRoot>(NewEvent(aggregateRootId, 3));
            _factory.SaveEvent<SomeEvent, SomeRoot>(NewEvent(aggregateRootId, 4));

            var instance = _repo.Get<SomeRoot>(aggregateRootId, new InMemoryUnitOfWork()).AggregateRoot;

            Assert.That(instance.EventCounter, Is.EqualTo(4));
        }

        static SomeEvent NewEvent(Guid aggregateRootId, int sequenceNumber)
        {
            return new SomeEvent
            {
                Meta =
                {
                    { DomainEvent.MetadataKeys.AggregateRootId, aggregateRootId},
                    { DomainEvent.MetadataKeys.SequenceNumber, sequenceNumber},
                }
            };
        }

        public class SomeEvent : DomainEvent<SomeRoot>
        {
        }

        public class SomeRoot : AggregateRoot, IEmit<SomeEvent>
        {
            public int EventCounter { get; set; }

            public void Apply(SomeEvent e)
            {
                EventCounter++;
            }
        }
    }
}
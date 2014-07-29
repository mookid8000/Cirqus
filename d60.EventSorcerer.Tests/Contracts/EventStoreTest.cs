using System;
using System.Linq;
using d60.EventSorcerer.Events;
using d60.EventSorcerer.MongoDb.Events;
using d60.EventSorcerer.MsSql;
using d60.EventSorcerer.Numbers;
using d60.EventSorcerer.Tests.MongoDb;
using d60.EventSorcerer.Tests.Stubs;
using NUnit.Framework;

namespace d60.EventSorcerer.Tests.Contracts
{
    [Description("Contract test for event stores. Verifies that event store implementation and sequence number generation works in tandem")]
    [TestFixture(typeof(MongoDbEventStoreFactory), Category = TestCategories.MongoDb)]
    [TestFixture(typeof(InMemoryEventStoreFactory))]
    [TestFixture(typeof(MsSqlEventStoreFactory))]
    public class EventStoreTest<TEventStoreFactory> : FixtureBase where TEventStoreFactory : IEventStoreFactory, new()
    {
        TEventStoreFactory _eventStoreFactory;
        IEventStore _eventStore;

        protected override void DoSetUp()
        {
            _eventStoreFactory = new TEventStoreFactory();

            _eventStore = _eventStoreFactory.GetEventStore();
        }

        [Test]
        public void CanSaveEventBatch()
        {
            // arrange
            var batchId = Guid.NewGuid();
            var aggregateRootId = Guid.NewGuid();

            var events = new DomainEvent[] {new SomeEvent
            {
                SomeValue = "hej",
                Meta =
                {
                    {DomainEvent.MetadataKeys.SequenceNumber, 1},
                    {DomainEvent.MetadataKeys.AggregateRootId, aggregateRootId}
                }
            }};

            // act
            _eventStore.Save(batchId, events);

            // assert
            var persistedEvents = _eventStore.Load(aggregateRootId);
            var someEvent = (SomeEvent)persistedEvents.Single();

            Assert.That(someEvent.SomeValue, Is.EqualTo("hej"));
        }

        [Test]
        public void ValidatesPresenceOfSequenceNumbers()
        {
            // arrange
            var batchId = Guid.NewGuid();

            var events = new DomainEvent[]
            {
                new SomeEvent
                {
                    SomeValue = "hej",
                    Meta = {
                        {DomainEvent.MetadataKeys.AggregateRootId, Guid.NewGuid()},
                        //{DomainEvent.MetadataKeys.SequenceNumber, 1}, //< this one is missing!
                    } 
                }
            };

            // act
            // assert
            var ex = Assert.Throws<InvalidOperationException>(() => _eventStore.Save(batchId, events));

            Console.WriteLine(ex);
        }

        [Test]
        public void ValidatesPresenceOfAggregateRootId()
        {
            // arrange
            var batchId = Guid.NewGuid();

            var events = new DomainEvent[]
            {
                new SomeEvent
                {
                    SomeValue = "hej",
                    Meta = {
                        //{DomainEvent.MetadataKeys.AggregateRootId, Guid.NewGuid()}, //< this one is missing!
                        {DomainEvent.MetadataKeys.SequenceNumber, 1},
                    } 
                }
            };

            // act
            // assert
            var ex = Assert.Throws<InvalidOperationException>(() => _eventStore.Save(batchId, events));

            Console.WriteLine(ex);
        }

        [Test]
        public void ValidatesSequenceOfSequenceNumbers()
        {
            // arrange
            var batchId = Guid.NewGuid();

            var events = new DomainEvent[]
            {
                new SomeEvent
                {
                    SomeValue = "hej",
                    Meta = {{DomainEvent.MetadataKeys.SequenceNumber, 1}}
                },
                new SomeEvent
                {
                    SomeValue = "hej",
                    Meta = {{DomainEvent.MetadataKeys.SequenceNumber, 2}}
                },
                new SomeEvent
                {
                    SomeValue = "hej",
                    Meta = {{DomainEvent.MetadataKeys.SequenceNumber, 4}}
                }
            };

            // act
            // assert
            var ex = Assert.Throws<InvalidOperationException>(() => _eventStore.Save(batchId, events));

            Console.WriteLine(ex);
        }

        [Test]
        public void SavedSequenceNumbersAreUnique()
        {
            // arrange

            var aggregateRootId = Guid.NewGuid();
            var events = new[]
            {
                Event(1, aggregateRootId), 
                Event(2, aggregateRootId), 
                Event(3, aggregateRootId),
            };

            _eventStore.Save(Guid.NewGuid(), events);

            var batchWithAlreadyUsedSequenceNumber = new[] { Event(2, aggregateRootId) };

            // act
            // assert
            var ex = Assert.Throws<ConcurrencyException>(() => _eventStore.Save(Guid.NewGuid(), batchWithAlreadyUsedSequenceNumber));

            Console.WriteLine(ex);
        }

        [Test]
        public void SavedSequenceNumbersAreUniqueScopedToAggregateRoot()
        {
            // arrange
            var agg1Id = Guid.NewGuid();
            var agg2Id = Guid.NewGuid();

            var events = new[]
            {
                Event(1, agg1Id), 
                Event(2, agg1Id), 
                Event(3, agg1Id),
            };

            _eventStore.Save(Guid.NewGuid(), events);

            var batchWithAlreadyUsedSequenceNumberOnlyForAnotherAggregate = new[]
            {
                Event(1, agg2Id),
                Event(2, agg2Id)
            };

            // act
            // assert
            Assert.DoesNotThrow(() => _eventStore.Save(Guid.NewGuid(), batchWithAlreadyUsedSequenceNumberOnlyForAnotherAggregate));

            var batchWithAlreadyUsedSequenceNumber = new[]
            {
                Event(4, agg1Id),
                Event(2, agg2Id)
            };

            var ex = Assert.Throws<ConcurrencyException>(() => _eventStore.Save(Guid.NewGuid(), batchWithAlreadyUsedSequenceNumber));
        }

        [Test]
        public void CanLoadEvents()
        {
            // arrange
            var aggregateRootId = Guid.NewGuid();
            _eventStore.Save(Guid.NewGuid(), new[]
            {
                Event(1, aggregateRootId),
                Event(2, aggregateRootId),
                Event(3, aggregateRootId),
                Event(4, aggregateRootId),
                Event(5, aggregateRootId),
                Event(6, aggregateRootId)
            });
            _eventStore.Save(Guid.NewGuid(), new[]
            {
                Event(7, aggregateRootId),
                Event(8, aggregateRootId),
                Event(9, aggregateRootId),
                Event(10, aggregateRootId),
                Event(11, aggregateRootId),
                Event(12, aggregateRootId)
            });
            _eventStore.Save(Guid.NewGuid(), new[]
            {
                Event(13, aggregateRootId),
                Event(14, aggregateRootId),
                Event(15, aggregateRootId)
            });

            // act
            // assert
            Assert.That(_eventStore.Load(aggregateRootId, 1, 1).Count(), Is.EqualTo(1));
            Assert.That(_eventStore.Load(aggregateRootId, 1, 1).GetSeq(), Is.EqualTo(Enumerable.Range(1, 1)));

            Assert.That(_eventStore.Load(aggregateRootId, 1, 2).Count(), Is.EqualTo(2));
            Assert.That(_eventStore.Load(aggregateRootId, 1, 2).GetSeq(), Is.EqualTo(Enumerable.Range(1, 2)));

            Assert.That(_eventStore.Load(aggregateRootId, 1, 10).Count(), Is.EqualTo(10));
            Assert.That(_eventStore.Load(aggregateRootId, 1, 10).GetSeq(), Is.EqualTo(Enumerable.Range(1, 10)));

            Assert.That(_eventStore.Load(aggregateRootId, 4, 10).Count(), Is.EqualTo(10));
            Assert.That(_eventStore.Load(aggregateRootId, 4, 10).GetSeq(), Is.EqualTo(Enumerable.Range(4, 10)));
        }

        [Test]
        public void CanGetNextSequenceNumber()
        {
            var agg1Id = Guid.NewGuid();
            var agg2Id = Guid.NewGuid();

            var generator = _eventStoreFactory.GetSequenceNumberGenerator();

            Assert.That(generator.Next(agg1Id), Is.EqualTo(0));
            Assert.That(generator.Next(agg1Id), Is.EqualTo(0));
            Assert.That(generator.Next(agg2Id), Is.EqualTo(0));
            Assert.That(generator.Next(agg2Id), Is.EqualTo(0));

            _eventStore.Save(Guid.NewGuid(), new[]
            {
                Event(0, agg1Id)
            });

            Assert.That(generator.Next(agg1Id), Is.EqualTo(1), "Expected the seq for {0} to have been incremented once", agg1Id);
            Assert.That(generator.Next(agg2Id), Is.EqualTo(0), "Expected the seq for {0} to not have been changed", agg2Id);

            _eventStore.Save(Guid.NewGuid(), new[]
            {
                Event(1, agg1Id), 
                Event(2, agg1Id), 
                Event(3, agg1Id)
            });

            Assert.That(generator.Next(agg1Id), Is.EqualTo(4), "Expected the seq for {0} to have been incremented four times", agg1Id);
            Assert.That(generator.Next(agg2Id), Is.EqualTo(0), "Expected the seq for {0} to not have been changed", agg2Id);

            _eventStore.Save(Guid.NewGuid(), new[]
            {
                Event(0, agg2Id)
            });

            Assert.That(generator.Next(agg1Id), Is.EqualTo(4), "Expected the seq for {0} to have been incremented four times", agg1Id);
            Assert.That(generator.Next(agg2Id), Is.EqualTo(1), "Expected the seq for {0} to have been incremented once", agg2Id);
        }

        [Test]
        public void CanLoadEventsByAggregateRootId()
        {
            // arrange
            var agg1 = Guid.NewGuid();
            var agg2 = Guid.NewGuid();
            _eventStore.Save(Guid.NewGuid(), new[]
            {
                Event(1, agg1),
                Event(2, agg1),
                Event(3, agg2)
            });
            _eventStore.Save(Guid.NewGuid(), new[]
            {
                Event(4, agg1),
                Event(5, agg1),
                Event(6, agg2)
            });

            // act
            var allEventsForAgg1 = _eventStore.Load(agg1, 0, int.MaxValue).ToList();
            var allEventsForAgg2 = _eventStore.Load(agg2, 0, int.MaxValue).ToList();

            // assert
            Assert.That(allEventsForAgg1.Count, Is.EqualTo(4));
            Assert.That(allEventsForAgg1.GetSeq(), Is.EqualTo(new[] { 1, 2, 4, 5 }));

            Assert.That(allEventsForAgg2.Count, Is.EqualTo(2));
            Assert.That(allEventsForAgg2.GetSeq(), Is.EqualTo(new[] { 3, 6 }));
        }

        static DomainEvent Event(int seq, Guid aggregateRootId)
        {
            return new SomeEvent
            {
                SomeValue = "hej",
                Meta =
                {
                    { DomainEvent.MetadataKeys.SequenceNumber, seq },
                    { DomainEvent.MetadataKeys.AggregateRootId, aggregateRootId }
                }
            };
        }

        class SomeEvent : DomainEvent
        {
            public string SomeValue { get; set; }
        }
    }

    public class InMemoryEventStoreFactory : IEventStoreFactory
    {
        readonly InMemoryEventStore _eventStore;

        public InMemoryEventStoreFactory()
        {
            _eventStore = new InMemoryEventStore();
        }

        public IEventStore GetEventStore()
        {
            return _eventStore;
        }

        public ISequenceNumberGenerator GetSequenceNumberGenerator()
        {
            return _eventStore;
        }
    }

    public class MongoDbEventStoreFactory : IEventStoreFactory
    {
        readonly MongoDbEventStore _eventStore;

        public MongoDbEventStoreFactory()
        {
            _eventStore = new MongoDbEventStore(Helper.InitializeTestDatabase(), "events");
        }

        public IEventStore GetEventStore()
        {
            return _eventStore;
        }

        public ISequenceNumberGenerator GetSequenceNumberGenerator()
        {
            return _eventStore;
        }
    }
    public class MsSqlEventStoreFactory : IEventStoreFactory
    {
        readonly MsSqlEventStore _eventStore;

        public MsSqlEventStoreFactory()
        {
            _eventStore = new MsSqlEventStore();
        }

        public IEventStore GetEventStore()
        {
            return _eventStore;
        }

        public ISequenceNumberGenerator GetSequenceNumberGenerator()
        {
            return _eventStore;
        }
    }

    public interface IEventStoreFactory
    {
        IEventStore GetEventStore();
        ISequenceNumberGenerator GetSequenceNumberGenerator();
    }
}
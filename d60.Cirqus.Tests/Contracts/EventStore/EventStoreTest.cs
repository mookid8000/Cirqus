using System;
using System.Linq;
using d60.Cirqus.Aggregates;
using d60.Cirqus.Commands;
using d60.Cirqus.Config;
using d60.Cirqus.Events;
using d60.Cirqus.Exceptions;
using d60.Cirqus.Extensions;
using d60.Cirqus.Logging;
using d60.Cirqus.Logging.Null;
using d60.Cirqus.Numbers;
using d60.Cirqus.Tests.Contracts.EventStore.Factories;
using d60.Cirqus.Tests.Stubs;
using NUnit.Framework;

namespace d60.Cirqus.Tests.Contracts.EventStore
{
    [Description("Contract test for event stores. Verifies that event store implementation and sequence number generation works in tandem")]
    [TestFixture(typeof(MongoDbEventStoreFactory), Category = TestCategories.MongoDb)]
    [TestFixture(typeof(InMemoryEventStoreFactory))]
    [TestFixture(typeof(MsSqlEventStoreFactory), Category = TestCategories.MsSql)]
    [TestFixture(typeof(PostgreSqlEventStoreFactory), Category = TestCategories.PostgreSql)]
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
        public void TimeStampsCanRoundtripAsTheyShould()
        {
            var someLocalTime = new DateTime(2015, 10, 31, 12, 10, 15, DateTimeKind.Local);
            var someUtcTime = someLocalTime.ToUniversalTime();
            TimeMachine.FixCurrentTimeTo(someUtcTime);
            
            var processor = new CommandProcessor(_eventStore, new DefaultAggregateRootRepository(_eventStore), new ConsoleOutEventDispatcher());
            
            RegisterForDisposal(processor);
            
            processor.ProcessCommand(new MakeSomeRootEmitTheEvent(Guid.NewGuid()));

            var domainEvents = _eventStore.Stream().Cast<SomeRootEvent>().Single();
            Assert.That(domainEvents.GetUtcTime(), Is.EqualTo(someUtcTime));
        }

        public class MakeSomeRootEmitTheEvent : Command<SomeRoot>
        {
            public MakeSomeRootEmitTheEvent(Guid aggregateRootId) : base(aggregateRootId)
            {
            }

            public override void Execute(SomeRoot aggregateRoot)
            {
                aggregateRoot.EmitTheEvent();
            }
        }

        public class SomeRoot : AggregateRoot, IEmit<SomeRootEvent>
        {
            public void EmitTheEvent()
            {
                Emit(new SomeRootEvent());
            }

            public void Apply(SomeRootEvent e)
            {
            }
        }

        public class SomeRootEvent : DomainEvent<SomeRoot> { }


        [Test]
        public void BatchIdIsAppliedAsMetadataToEvents()
        {
            // arrange

            // act
            var batch1 = Guid.NewGuid();
            var batch2 = Guid.NewGuid();

            _eventStore.Save(batch1, new[] { Event(0, Guid.NewGuid()), Event(0, Guid.NewGuid()) });
            _eventStore.Save(batch2, new[] { Event(0, Guid.NewGuid()), Event(0, Guid.NewGuid()), Event(0, Guid.NewGuid()) });

            // assert
            var allEvents = _eventStore
                .Stream()
                .OrderBy(a => a.GetGlobalSequenceNumber())
                .ToList();

            Assert.That(allEvents.Count, Is.EqualTo(5));

            var batches = allEvents
                .GroupBy(e => e.GetBatchId())
                .OrderBy(b => b.Count())
                .ToList();

            Assert.That(batches.Count, Is.EqualTo(2));

            Assert.That(batches[0].Key, Is.EqualTo(batch1));
            Assert.That(batches[1].Key, Is.EqualTo(batch2));

            Assert.That(batches[0].Count(), Is.EqualTo(2));
            Assert.That(batches[1].Count(), Is.EqualTo(3));
        }

        [Test]
        public void EventAreAutomaticallyGivenGlobalSequenceNumbers()
        {
            // arrange

            // act
            _eventStore.Save(Guid.NewGuid(), new[] { Event(0, Guid.NewGuid()) });
            _eventStore.Save(Guid.NewGuid(), new[] { Event(0, Guid.NewGuid()) });
            _eventStore.Save(Guid.NewGuid(), new[] { Event(0, Guid.NewGuid()) });
            _eventStore.Save(Guid.NewGuid(), new[] { Event(0, Guid.NewGuid()) });
            _eventStore.Save(Guid.NewGuid(), new[] { Event(0, Guid.NewGuid()) });

            // assert
            var allEvents = _eventStore
                .Stream()
                .Select(e => new
                {
                    Event = e,
                    GlobalSequenceNumber = e.GetGlobalSequenceNumber()
                })
                .OrderBy(a => a.GlobalSequenceNumber)
                .ToList();

            Assert.That(allEvents.Count, Is.EqualTo(5));
            Assert.That(allEvents.Select(a => a.GlobalSequenceNumber), Is.EqualTo(new[] { 0, 1, 2, 3, 4 }));
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

        [TestCase(100, 3)]
        [TestCase(1000, 10, Ignore = TestCategories.IgnoreLongRunning)]
        [TestCase(10000, 10, Ignore = TestCategories.IgnoreLongRunning)]
        [TestCase(1000, 100, Ignore = TestCategories.IgnoreLongRunning)]
        [TestCase(1000, 1000, Ignore = TestCategories.IgnoreLongRunning)]
        public void ComparePerformance(int numberOfBatches, int numberOfEventsPerBatch)
        {
            CirqusLoggerFactory.Current = new NullLoggerFactory();

            TakeTime(string.Format("Save {0} batches with {1} events in each", numberOfBatches, numberOfEventsPerBatch),
                () =>
                {
                    var seqNo = 0;
                    var id = Guid.NewGuid();
                    numberOfBatches.Times(() =>
                    {
                        var events = Enumerable
                            .Range(0, numberOfEventsPerBatch)
                            .Select(i => Event(seqNo++, id))
                            .ToList();

                        _eventStore.Save(Guid.NewGuid(), events);
                    });
                });
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
}
using System;
using System.Linq;
using System.Runtime.Serialization;
using d60.Cirqus.Aggregates;
using d60.Cirqus.Commands;
using d60.Cirqus.Events;
using d60.Cirqus.Exceptions;
using d60.Cirqus.Extensions;
using d60.Cirqus.Logging;
using d60.Cirqus.Logging.Null;
using d60.Cirqus.Numbers;
using d60.Cirqus.Serialization;
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
    [TestFixture(typeof(NtfsEventStoreFactory))]
    [TestFixture(typeof(SQLiteEventStoreFactory))]
    public class EventStoreTest<TEventStoreFactory> : FixtureBase where TEventStoreFactory : IEventStoreFactory, new()
    {
        readonly DomainEventSerializer _serializzle = new DomainEventSerializer();

        TEventStoreFactory _eventStoreFactory;
        IEventStore _eventStore;

        protected override void DoSetUp()
        {
            _eventStoreFactory = new TEventStoreFactory();

            _eventStore = _eventStoreFactory.GetEventStore();

            if (_eventStore is IDisposable)
            {
                RegisterForDisposal((IDisposable)_eventStore);
            }
        }

        [Test]
        public void TimeStampsCanRoundtripAsTheyShould()
        {
            var someLocalTime = new DateTime(2015, 10, 31, 12, 10, 15, DateTimeKind.Local);
            var someUtcTime = someLocalTime.ToUniversalTime();
            TimeMachine.FixCurrentTimeTo(someUtcTime);
            
            var processor = new CommandProcessor(_eventStore, new DefaultAggregateRootRepository(_eventStore), new ConsoleOutEventDispatcher(), new DomainEventSerializer());
            
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
                    {DomainEvent.MetadataKeys.SequenceNumber, 1.ToString(Metadata.NumberCulture)},
                    {DomainEvent.MetadataKeys.AggregateRootId, aggregateRootId.ToString()}
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
                        {DomainEvent.MetadataKeys.AggregateRootId, Guid.NewGuid().ToString()},
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
                        {DomainEvent.MetadataKeys.SequenceNumber, 1.ToString(Metadata.NumberCulture)},
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
                    Meta = {{DomainEvent.MetadataKeys.SequenceNumber, 1.ToString(Metadata.NumberCulture)}}
                },
                new SomeEvent
                {
                    SomeValue = "hej",
                    Meta = {{DomainEvent.MetadataKeys.SequenceNumber, 2.ToString(Metadata.NumberCulture)}}
                },
                new SomeEvent
                {
                    SomeValue = "hej",
                    Meta = {{DomainEvent.MetadataKeys.SequenceNumber, 4.ToString(Metadata.NumberCulture)}}
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
                NewEvent(0, aggregateRootId),
                NewEvent(1, aggregateRootId),
                NewEvent(2, aggregateRootId),
                NewEvent(3, aggregateRootId),
                NewEvent(4, aggregateRootId),
                NewEvent(5, aggregateRootId),
            });
            _eventStore.Save(Guid.NewGuid(), new[]
            {
                NewEvent(6, aggregateRootId),
                NewEvent(7, aggregateRootId),
                NewEvent(8, aggregateRootId),
                NewEvent(9, aggregateRootId),
                NewEvent(10, aggregateRootId),
                NewEvent(11, aggregateRootId),
            });
            _eventStore.Save(Guid.NewGuid(), new[]
            {
                NewEvent(12, aggregateRootId),
                NewEvent(13, aggregateRootId),
                NewEvent(14, aggregateRootId),
            });

            // act
            // assert
            Assert.That(_eventStore.LoadNew(aggregateRootId, 1).Take(1).Count(), Is.EqualTo(1));
            Assert.That(_eventStore.LoadNew(aggregateRootId, 1).Take(1).Select(Deserialized).GetSeq().ToArray(), Is.EqualTo(Enumerable.Range(1, 1).ToArray()));

            Assert.That(_eventStore.LoadNew(aggregateRootId, 1).Take(2).Count(), Is.EqualTo(2));
            Assert.That(_eventStore.LoadNew(aggregateRootId, 1).Take(2).Select(Deserialized).GetSeq(), Is.EqualTo(Enumerable.Range(1, 2)));

            Assert.That(_eventStore.LoadNew(aggregateRootId, 1).Take(10).Count(), Is.EqualTo(10));
            Assert.That(_eventStore.LoadNew(aggregateRootId, 1).Take(10).Select(Deserialized).GetSeq(), Is.EqualTo(Enumerable.Range(1, 10)));

            Assert.That(_eventStore.LoadNew(aggregateRootId, 4).Take(10).Count(), Is.EqualTo(10));
            Assert.That(_eventStore.LoadNew(aggregateRootId, 4).Take(10).Select(Deserialized).GetSeq().ToArray(), Is.EqualTo(Enumerable.Range(4, 10).ToArray()));
        }

        DomainEvent Deserialized(Event arg)
        {
            return _serializzle.DoDeserialize(arg);
        }

        [Test]
        public void CanLoadEventsByAggregateRootId()
        {
            // arrange
            var agg1 = Guid.NewGuid();
            var agg2 = Guid.NewGuid();
            _eventStore.Save(Guid.NewGuid(), new[]
            {
                NewEvent(0, agg1),
                NewEvent(1, agg1),
                NewEvent(2, agg2)
            });
            _eventStore.Save(Guid.NewGuid(), new[]
            {
                NewEvent(3, agg1),
                NewEvent(4, agg1),
                NewEvent(5, agg2)
            });

            // act
            var allEventsForAgg1 = _eventStore.LoadNew(agg1).ToList();
            var allEventsForAgg2 = _eventStore.LoadNew(agg2).ToList();

            // assert
            Assert.That(allEventsForAgg1.Count, Is.EqualTo(4));
            Assert.That(allEventsForAgg1.Select(Deserialized).GetSeq(), Is.EqualTo(new[] { 0, 1, 3, 4 }));

            Assert.That(allEventsForAgg2.Count, Is.EqualTo(2));
            Assert.That(allEventsForAgg2.Select(Deserialized).GetSeq(), Is.EqualTo(new[] { 2, 5 }));
        }

        [Test]
        public void SaveIsAtomic()
        {
            var agg1 = Guid.NewGuid();
            var agg2 = Guid.NewGuid();

            try
            {
                _eventStore.Save(Guid.NewGuid(), new[]
                {
                    Event(1, agg1),
                    Event(1, agg2),
                    new ThrowingEvent
                    {
                        Meta =
                        {
                            {DomainEvent.MetadataKeys.SequenceNumber, 2.ToString(Metadata.NumberCulture)},
                            {DomainEvent.MetadataKeys.AggregateRootId, agg2.ToString()}
                        }
                    }
                });
            }
            catch {
                // ignore it!
            }

            Assert.AreEqual(0, _eventStore.Stream().Count());
            Assert.AreEqual(0, _eventStore.Load(agg1).Count());
            Assert.AreEqual(0, _eventStore.Load(agg2).Count());
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(10)]
        public void CanGetNextGlobalSequenceNumber(int numberOfEvents)
        {
            for (var i = 0; i < numberOfEvents; i++)
            {
                _eventStore.Save(Guid.NewGuid(), new[]
                {
                    NewEvent(1, Guid.NewGuid()),
                });
            }

            var nextGlobalSequenceNumber = _eventStore.GetNextGlobalSequenceNumber();

            Assert.AreEqual(numberOfEvents, nextGlobalSequenceNumber);
        }

        [Test]
        public void LoadingFromEmptyStreamDoesNotFail()
        {
            Assert.AreEqual(0, _eventStore.StreamNew().Count());
            Assert.AreEqual(0, _eventStore.LoadNew(Guid.NewGuid()).Count());
        }

        [TestCase(100, 3)]
        [TestCase(1000, 10, Ignore = TestCategories.IgnoreLongRunning)]
        [TestCase(10000, 10, Ignore = TestCategories.IgnoreLongRunning)]
        [TestCase(1000, 100, Ignore = TestCategories.IgnoreLongRunning)]
        [TestCase(1000, 1000, Ignore = TestCategories.IgnoreLongRunning)]
        public void CompareSavePerformance(int numberOfBatches, int numberOfEventsPerBatch)
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

        [TestCase(1000)]
        [TestCase(10000, Ignore = TestCategories.IgnoreLongRunning)]
        [TestCase(100000, Ignore = TestCategories.IgnoreLongRunning)]
        public void CompareStreamPerformance(int numberOfEvents)
        {
            CirqusLoggerFactory.Current = new NullLoggerFactory();

            var seqNo = 0;
            var id = Guid.NewGuid();
            _eventStore.Save(Guid.NewGuid(), Enumerable.Range(0, numberOfEvents).Select(i => Event(seqNo++, id)));

            TakeTime(
                string.Format("Read stream of {0} events", numberOfEvents),
                () => _eventStore.Stream().ToList());
        }

        static DomainEvent Event(int seq, Guid aggregateRootId)
        {
            return new SomeEvent
            {
                SomeValue = "hej",
                Meta =
                {
                    { DomainEvent.MetadataKeys.SequenceNumber, seq.ToString(Metadata.NumberCulture) },
                    { DomainEvent.MetadataKeys.AggregateRootId, aggregateRootId.ToString() }
                }
            };
        }

        Event NewEvent(int seq, Guid aggregateRootId)
        {
            var domainEvent = new SomeEvent
            {
                SomeValue = "hej",
                Meta =
                {
                    { DomainEvent.MetadataKeys.SequenceNumber, seq.ToString(Metadata.NumberCulture) },
                    { DomainEvent.MetadataKeys.AggregateRootId, aggregateRootId.ToString() }
                }
            };

            return _serializzle.DoSerialize(domainEvent);
        }

        class SomeEvent : DomainEvent
        {
            public string SomeValue { get; set; }
        }

        class ThrowingEvent : DomainEvent
        {
            [OnSerializing()]
            internal void OnSerializingMethod(StreamingContext context)
            {
                throw new SerializationException("I ruin your batch!");
            }
        }
    }
}
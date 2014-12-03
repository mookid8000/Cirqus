using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Timers;
using d60.Cirqus.Aggregates;
using d60.Cirqus.Config;
using d60.Cirqus.Events;
using d60.Cirqus.Numbers;
using d60.Cirqus.Serialization;
using d60.Cirqus.Testing.Internals;
using d60.Cirqus.Tests.Stubs;
using NUnit.Framework;
using TestContext = d60.Cirqus.Testing.TestContext;

namespace d60.Cirqus.Tests.Aggregates
{
    [TestFixture]
    public class TestEventApplication
    {
        readonly JsonDomainEventSerializer _domainEventSerializer = new JsonDomainEventSerializer();
        readonly DefaultDomainTypeNameMapper _defaultDomainTypeNameMapper = new DefaultDomainTypeNameMapper();

        /// <summary>
        /// Without caching: Elapsed total: 00:00:03.0647447, hydrations/s: 32,6
        /// </summary>
        [TestCase(2000, 100)]
        public void TestRawApplicationPerformance(int numberOfEvents, int numberOfHydrations)
        {
            const string aggregateRootId = "bim";

            using (var context = new TestContext())
            {
                context.Asynchronous = true;

                Console.WriteLine("Saving {0} to history of '{1}'", numberOfEvents, aggregateRootId);

                using (var printer = new Timer(2000))
                {
                    var inserts = 0;
                    printer.Elapsed += delegate { Console.WriteLine("{0} events saved...", inserts); };
                    printer.Start();

                    foreach (var e in Enumerable.Range(0, numberOfEvents).Select(i => new SomeEvent(string.Format("Event {0}", i))))
                    {
                        context.Save(aggregateRootId, e);
                        inserts++;
                    }
                }

                Console.WriteLine("Hydrating {0} times", numberOfHydrations);
                var stopwatch = Stopwatch.StartNew();
                numberOfHydrations.Times(() =>
                {
                    using (var uow = context.BeginUnitOfWork())
                    {
                        var instance = uow.Load<SomeAggregate>(aggregateRootId);
                    }
                });
                
                var elapsed = stopwatch.Elapsed;
                Console.WriteLine("Elapsed total: {0}, hydrations/s: {1:0.0}", elapsed, numberOfHydrations/elapsed.TotalSeconds);
            }
        }

        [Test]
        public void AppliesEmittedEvents()
        {
            var aggregateRootRepository = CreateAggregateRootRepository();
            var someAggregate = new SomeAggregate
            {
                UnitOfWork = new ConsoleOutUnitOfWork(aggregateRootRepository),
            };
            someAggregate.Initialize("root_id");

            someAggregate.DoSomething();

            Assert.That(someAggregate.StuffThatWasDone.Count, Is.EqualTo(1));
            Assert.That(someAggregate.StuffThatWasDone.First(), Is.EqualTo("emitted an event"));
        }

        [Test]
        public void ProvidesSuitableMetadataOnEvents()
        {
            var timeForFirstEvent = new DateTime(1979, 3, 19, 19, 0, 0, DateTimeKind.Utc);
            var timeForNextEvent = timeForFirstEvent.AddMilliseconds(2);

            var aggregateRootRepository = CreateAggregateRootRepository();
            var eventCollector = new InMemoryUnitOfWork(aggregateRootRepository, _defaultDomainTypeNameMapper);

            var someAggregate = new SomeAggregate
            {
                UnitOfWork = eventCollector,
            };
            someAggregate.Initialize("root_id");

            TimeMachine.FixCurrentTimeTo(timeForFirstEvent);

            someAggregate.DoSomething();

            TimeMachine.FixCurrentTimeTo(timeForNextEvent);

            someAggregate.DoSomething();

            var events = eventCollector.Cast<SomeEvent>().ToList();
            var firstEvent = events[0];

            Assert.That(firstEvent.Meta[DomainEvent.MetadataKeys.TimeUtc], Is.EqualTo(timeForFirstEvent.ToString("u")));
            Assert.That(firstEvent.Meta[DomainEvent.MetadataKeys.Owner], Is.EqualTo("d60.Cirqus.Tests.Aggregates.TestEventApplication+SomeAggregate, d60.Cirqus.Tests"));
            Assert.That(firstEvent.Meta[DomainEvent.MetadataKeys.Type], Is.EqualTo("d60.Cirqus.Tests.Aggregates.TestEventApplication+SomeEvent, d60.Cirqus.Tests"));
            Assert.That(firstEvent.Meta[DomainEvent.MetadataKeys.SequenceNumber], Is.EqualTo("0"));
            Assert.That(firstEvent.Meta[DomainEvent.MetadataKeys.AggregateRootId], Is.EqualTo("root_id"));

            var nextEvent = events[1];

            Assert.That(nextEvent.Meta[DomainEvent.MetadataKeys.TimeUtc], Is.EqualTo(timeForNextEvent.ToString("u")));
            Assert.That(nextEvent.Meta[DomainEvent.MetadataKeys.Owner], Is.EqualTo("d60.Cirqus.Tests.Aggregates.TestEventApplication+SomeAggregate, d60.Cirqus.Tests"));
            Assert.That(nextEvent.Meta[DomainEvent.MetadataKeys.Type], Is.EqualTo("d60.Cirqus.Tests.Aggregates.TestEventApplication+SomeEvent, d60.Cirqus.Tests"));
            Assert.That(nextEvent.Meta[DomainEvent.MetadataKeys.SequenceNumber], Is.EqualTo("1"));
            Assert.That(nextEvent.Meta[DomainEvent.MetadataKeys.AggregateRootId], Is.EqualTo("root_id"));
        }

        [Test]
        public void FailsOnSequenceMismatch()
        {
            var someAggregate = new SomeAggregate();

            var @eventWithTooLateSeqNumber = new SomeEvent("something");

            // some global seq - not important
            @eventWithTooLateSeqNumber.Meta[DomainEvent.MetadataKeys.GlobalSequenceNumber] = 10.ToString();
            
            // local seq that are too far ahead
            @eventWithTooLateSeqNumber.Meta[DomainEvent.MetadataKeys.SequenceNumber] = 1.ToString();

            Assert.Throws<ApplicationException>(() => someAggregate.ApplyEvent(@eventWithTooLateSeqNumber, ReplayState.ReplayApply));
        }

        DefaultAggregateRootRepository CreateAggregateRootRepository()
        {
            var inMemoryEventStore = new InMemoryEventStore(_domainEventSerializer);

            return new DefaultAggregateRootRepository(inMemoryEventStore, _domainEventSerializer, _defaultDomainTypeNameMapper);
        }

        class SomeEvent : DomainEvent<SomeAggregate>
        {
            public readonly string What;

            public SomeEvent(string what)
            {
                What = what;
            }
        }

        class SomeAggregate : AggregateRoot, IEmit<SomeEvent>
        {
            public readonly List<string> StuffThatWasDone = new List<string>();
        
            public void DoSomething()
            {
                Emit(new SomeEvent("emitted an event"));
            }

            public void Apply(SomeEvent e)
            {
                StuffThatWasDone.Add(e.What);
            }
        }

    }

}
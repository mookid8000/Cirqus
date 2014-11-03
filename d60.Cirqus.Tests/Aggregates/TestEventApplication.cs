using System;
using System.Collections.Generic;
using System.Linq;
using d60.Cirqus.Aggregates;
using d60.Cirqus.Events;
using d60.Cirqus.Numbers;
using d60.Cirqus.Serialization;
using d60.Cirqus.Testing.Internals;
using d60.Cirqus.Tests.Stubs;
using NUnit.Framework;

namespace d60.Cirqus.Tests.Aggregates
{
    [TestFixture]
    public class TestEventApplication
    {
        readonly JsonDomainEventSerializer _domainEventSerializer = new JsonDomainEventSerializer();

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
            var eventCollector = new InMemoryUnitOfWork(aggregateRootRepository);

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
            Assert.That(firstEvent.Meta[DomainEvent.MetadataKeys.SequenceNumber], Is.EqualTo("0"));
            Assert.That(firstEvent.Meta[DomainEvent.MetadataKeys.AggregateRootId], Is.EqualTo("root_id"));

            var nextEvent = events[1];

            Assert.That(nextEvent.Meta[DomainEvent.MetadataKeys.TimeUtc], Is.EqualTo(timeForNextEvent.ToString("u")));
            Assert.That(nextEvent.Meta[DomainEvent.MetadataKeys.Owner], Is.EqualTo("d60.Cirqus.Tests.Aggregates.TestEventApplication+SomeAggregate, d60.Cirqus.Tests"));
            Assert.That(nextEvent.Meta[DomainEvent.MetadataKeys.SequenceNumber], Is.EqualTo("1"));
            Assert.That(nextEvent.Meta[DomainEvent.MetadataKeys.AggregateRootId], Is.EqualTo("root_id"));
        }

        DefaultAggregateRootRepository CreateAggregateRootRepository()
        {
            var inMemoryEventStore = new InMemoryEventStore(_domainEventSerializer);

            return new DefaultAggregateRootRepository(inMemoryEventStore, _domainEventSerializer);
        }


        public class SomeEvent : DomainEvent<SomeAggregate>
        {
            public readonly string What;

            public SomeEvent(string what)
            {
                What = what;
            }
        }

        public class SomeAggregate : AggregateRoot, IEmit<SomeEvent>
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
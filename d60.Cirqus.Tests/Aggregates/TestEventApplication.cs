using System;
using System.Collections.Generic;
using System.Linq;
using d60.Cirqus.Aggregates;
using d60.Cirqus.Events;
using d60.Cirqus.Numbers;
using d60.Cirqus.Testing.Internals;
using d60.Cirqus.Tests.Stubs;
using NUnit.Framework;

namespace d60.Cirqus.Tests.Aggregates
{
    [TestFixture]
    public class TestEventApplication
    {
        [Test]
        public void AppliesEmittedEvents()
        {
            var someAggregate = new SomeAggregate
            {
                UnitOfWork = new ConsoleOutUnitOfWork(),
            };
            someAggregate.Initialize(Guid.NewGuid());
            someAggregate.AggregateRootRepository = new DefaultAggregateRootRepository(new InMemoryEventStore());

            someAggregate.DoSomething();

            Assert.That(someAggregate.StuffThatWasDone.Count, Is.EqualTo(1));
            Assert.That(someAggregate.StuffThatWasDone.First(), Is.EqualTo("emitted an event"));
        }

        [Test]
        public void ProvidesSuitableMetadataOnEvents()
        {
            var timeForFirstEvent = new DateTime(1979, 3, 19, 19, 0, 0, DateTimeKind.Utc);
            var timeForNextEvent = timeForFirstEvent.AddMilliseconds(2);

            var aggregateRootId = Guid.NewGuid();
            var eventCollector = new InMemoryUnitOfWork();

            var someAggregate = new SomeAggregate
            {
                UnitOfWork = eventCollector,
            };
            someAggregate.Initialize(aggregateRootId);
            someAggregate.AggregateRootRepository = new DefaultAggregateRootRepository(new InMemoryEventStore());

            TimeMachine.FixCurrentTimeTo(timeForFirstEvent);

            someAggregate.DoSomething();

            TimeMachine.FixCurrentTimeTo(timeForNextEvent);

            someAggregate.DoSomething();

            var events = eventCollector.Cast<SomeEvent>().ToList();
            var firstEvent = events[0];

            Assert.That(firstEvent.Meta[DomainEvent.MetadataKeys.TimeUtc], Is.EqualTo(timeForFirstEvent.ToString("u")));
            Assert.That(firstEvent.Meta[DomainEvent.MetadataKeys.Owner], Is.EqualTo("d60.Cirqus.Tests.Aggregates.TestEventApplication+SomeAggregate, d60.Cirqus.Tests"));
            Assert.That(firstEvent.Meta[DomainEvent.MetadataKeys.SequenceNumber], Is.EqualTo(0));
            Assert.That(firstEvent.Meta[DomainEvent.MetadataKeys.AggregateRootId], Is.EqualTo(aggregateRootId));

            var nextEvent = events[1];

            Assert.That(nextEvent.Meta[DomainEvent.MetadataKeys.TimeUtc], Is.EqualTo(timeForNextEvent.ToString("u")));
            Assert.That(nextEvent.Meta[DomainEvent.MetadataKeys.Owner], Is.EqualTo("d60.Cirqus.Tests.Aggregates.TestEventApplication+SomeAggregate, d60.Cirqus.Tests"));
            Assert.That(nextEvent.Meta[DomainEvent.MetadataKeys.SequenceNumber], Is.EqualTo(1));
            Assert.That(nextEvent.Meta[DomainEvent.MetadataKeys.AggregateRootId], Is.EqualTo(aggregateRootId));
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
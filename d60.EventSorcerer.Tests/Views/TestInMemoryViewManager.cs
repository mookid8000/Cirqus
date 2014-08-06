using System;
using System.Linq;
using d60.EventSorcerer.Aggregates;
using d60.EventSorcerer.Events;
using d60.EventSorcerer.TestHelpers.Internals;
using d60.EventSorcerer.Views.Basic;
using d60.EventSorcerer.Views.Basic.Locators;
using NUnit.Framework;

namespace d60.EventSorcerer.Tests.Views
{
    [TestFixture]
    public class TestInMemoryViewManager : FixtureBase
    {
        InMemoryViewManager<SomeView> _viewManager;
        BasicEventDispatcher _eventDispatcher;
        InMemoryEventStore _inMemoryEventStore;

        protected override void DoSetUp()
        {
            _viewManager = new InMemoryViewManager<SomeView>();
            _inMemoryEventStore = new InMemoryEventStore();
            _eventDispatcher = new BasicEventDispatcher(new BasicAggregateRootRepository(_inMemoryEventStore), new IViewManager[] { _viewManager });
        }

        [Test]
        public void CanDispatchEvents()
        {
            var firstRoot = Guid.NewGuid();
            var secondRoot = Guid.NewGuid();

            _eventDispatcher.Dispatch(_inMemoryEventStore,  new DomainEvent[] { EventFor(firstRoot) });
            _eventDispatcher.Dispatch(_inMemoryEventStore, new DomainEvent[] { EventFor(firstRoot) });
            _eventDispatcher.Dispatch(_inMemoryEventStore, new DomainEvent[] { EventFor(secondRoot) });

            var viewInstances = _viewManager.ToList();

            Assert.That(viewInstances.Count, Is.EqualTo(2));

            Assert.That(viewInstances.Count(i => i.AggregateRootId == firstRoot), Is.EqualTo(1),
                "Expected one single view instance for aggregate root {0}", firstRoot);

            Assert.That(viewInstances.Single(i => i.AggregateRootId == firstRoot).NumberOfEventsHandled, Is.EqualTo(2),
                "Expected two events to have been processed");

            Assert.That(viewInstances.Count(i => i.AggregateRootId == secondRoot), Is.EqualTo(1),
                "Expected one single view instance for aggregate root {0}", secondRoot);

            Assert.That(viewInstances.Single(i => i.AggregateRootId == secondRoot).NumberOfEventsHandled, Is.EqualTo(1),
                "Expected one event to have been processed");
        }

        /// <summary>
        /// Initial:
        /// Dispatch 1000000 events - elapsed: 5.9 s
        /// 
        /// After caching of eventDispatcher methods per domain event type:
        /// Dispatch 1000000 events - elapsed: 2.5 s
        /// 
        /// Can possibly be optimized even more
        /// </summary>
        [TestCase(100000)]
        [TestCase(1000000, Ignore = false, Description = "chill, dude")]
        public void CheckPerformance(int numberOfEvents)
        {
            var firstRoot = Guid.NewGuid();

            TakeTime("Dispatch " + numberOfEvents + " events",
                () => numberOfEvents.Times(() => _eventDispatcher.Dispatch(_inMemoryEventStore, new DomainEvent[] { EventFor(firstRoot) })));
        }


        static SomeEvent EventFor(Guid newGuid)
        {
            var e = new SomeEvent();
            e.Meta[DomainEvent.MetadataKeys.AggregateRootId] = newGuid;
            return e;
        }

        class SomeEvent : DomainEvent
        {

        }

        class SomeView : IView<InstancePerAggregateRootLocator>, ISubscribeTo<SomeEvent>
        {
            public SomeView()
            {
                NumberOfEventsHandled = 0;
            }

            public string Id { get; set; }
            public long LastGlobalSequenceNumber { get; set; }

            public Guid AggregateRootId { get; set; }

            public int NumberOfEventsHandled { get; set; }

            public void Handle(IViewContext context, SomeEvent domainEvent)
            {
                AggregateRootId = new Guid(domainEvent.Meta[DomainEvent.MetadataKeys.AggregateRootId].ToString());

                NumberOfEventsHandled++;
            }
        }
    }
}
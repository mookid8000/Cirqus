using System;
using System.Linq;
using d60.Cirqus.Aggregates;
using d60.Cirqus.Events;
using d60.Cirqus.Logging;
using d60.Cirqus.Logging.Console;
using d60.Cirqus.TestHelpers.Internals;
using d60.Cirqus.Views.ViewManagers;
using d60.Cirqus.Views.ViewManagers.Locators;
using d60.Cirqus.Views.ViewManagers.Old;
using NUnit.Framework;
using ViewManagerEventDispatcher = d60.Cirqus.Views.ViewManagers.Old.ViewManagerEventDispatcher;

namespace d60.Cirqus.Tests.Views
{
    [TestFixture]
    public class TestInMemoryViewManager : FixtureBase
    {
        Cirqus.Views.ViewManagers.Old.InMemoryViewManager<SomeViewInstance> _viewManager;
        ViewManagerEventDispatcher _eventDispatcher;
        InMemoryEventStore _eventStore;
        long _currentSequenceNumber;

        protected override void DoSetUp()
        {
            _viewManager = new Cirqus.Views.ViewManagers.Old.InMemoryViewManager<SomeViewInstance>();
            _eventStore = new InMemoryEventStore();
            _eventDispatcher = new ViewManagerEventDispatcher(new DefaultAggregateRootRepository(_eventStore), new IViewManager[] { _viewManager });
            _eventDispatcher.Initialize(_eventStore);
        }

        [Test]
        public void CanDispatchEvents()
        {
            var firstRoot = Guid.NewGuid();
            var secondRoot = Guid.NewGuid();

            _eventDispatcher.Dispatch(_eventStore,  new DomainEvent[] { EventFor(firstRoot) });
            _eventDispatcher.Dispatch(_eventStore, new DomainEvent[] { EventFor(firstRoot) });
            _eventDispatcher.Dispatch(_eventStore, new DomainEvent[] { EventFor(secondRoot) });

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
            CirqusLoggerFactory.Current = new ConsoleLoggerFactory(minLevel: Logger.Level.Info);

            var firstRoot = Guid.NewGuid();

            TakeTime("Dispatch " + numberOfEvents + " events",
                () => numberOfEvents.Times(() => _eventDispatcher.Dispatch(_eventStore, new DomainEvent[] { EventFor(firstRoot) })));
        }

        SomeEvent EventFor(Guid newGuid)
        {
            var e = new SomeEvent();
            e.Meta[DomainEvent.MetadataKeys.AggregateRootId] = newGuid;
            e.Meta[DomainEvent.MetadataKeys.GlobalSequenceNumber] = _currentSequenceNumber++;
            return e;
        }

        class SomeEvent : DomainEvent
        {

        }

        class SomeViewInstance : IViewInstance<InstancePerAggregateRootLocator>, ISubscribeTo<SomeEvent>
        {
            public SomeViewInstance()
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
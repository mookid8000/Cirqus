using System;
using System.Linq;
using d60.EventSorcerer.Events;
using d60.EventSorcerer.MongoDb.Views;
using d60.EventSorcerer.TestHelpers;
using d60.EventSorcerer.Views.Basic;
using d60.EventSorcerer.Views.Basic.Locators;
using MongoDB.Driver;
using NUnit.Framework;

namespace d60.EventSorcerer.Tests.MongoDb.Views
{
    [TestFixture]
    [Category(TestCategories.MongoDb)]
    public class TestMongoDbViewManager : FixtureBase
    {
        MongoDatabase _database;
        BasicEventDispatcher _eventDispatcher;
        MongoDbViewManager<SomeView> _viewManager;

        protected override void DoSetUp()
        {
            _database = Helper.InitializeTestDatabase();
            _viewManager = new MongoDbViewManager<SomeView>(_database, "views");
            _eventDispatcher = new BasicEventDispatcher(new IViewManager[] { _viewManager });
        }

        [Test]
        public void CanLoadViewsAsWell()
        {
            var firstRoot = Guid.NewGuid();
            var expectedViewId = InstancePerAggregateRootLocator.GetViewIdFromGuid(firstRoot);

            _eventDispatcher.Dispatch(new InMemoryEventStore(),
                new DomainEvent[]
            {
                EventFor(firstRoot),
                EventFor(firstRoot),
                EventFor(firstRoot),
            });

            var view = _viewManager.Load(expectedViewId);

            Assert.That(view.NumberOfEventsHandled, Is.EqualTo(3));
        }


        [Test]
        public void CanDispatchEvents()
        {
            var firstRoot = Guid.NewGuid();
            var secondRoot = Guid.NewGuid();

            _eventDispatcher.Dispatch(new InMemoryEventStore(), new DomainEvent[] { EventFor(firstRoot) });
            _eventDispatcher.Dispatch(new InMemoryEventStore(), new DomainEvent[] { EventFor(firstRoot) });
            _eventDispatcher.Dispatch(new InMemoryEventStore(), new DomainEvent[] { EventFor(secondRoot) });

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

        [TestCase(1000)]
        [TestCase(10000)]
        public void CheckPerformance(int numberOfEvents)
        {
            var firstRoot = Guid.NewGuid();

            TakeTime("Dispatch " + numberOfEvents + " events",
                () => numberOfEvents.Times(() => _eventDispatcher.Dispatch(new InMemoryEventStore(), new DomainEvent[] { EventFor(firstRoot) })));
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

            public Guid AggregateRootId { get; set; }

            public int NumberOfEventsHandled { get; set; }

            public void Handle(SomeEvent domainEvent)
            {
                AggregateRootId = new Guid(domainEvent.Meta[DomainEvent.MetadataKeys.AggregateRootId].ToString());

                NumberOfEventsHandled++;
            }

            public string Id { get; set; }
        }

    }
}
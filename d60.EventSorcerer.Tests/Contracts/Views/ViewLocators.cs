using System;
using d60.EventSorcerer.Events;
using d60.EventSorcerer.MongoDb.Events;
using d60.EventSorcerer.Tests.Contracts.Views.Factories;
using d60.EventSorcerer.Tests.MongoDb;
using d60.EventSorcerer.Tests.Stubs;
using d60.EventSorcerer.Views.Basic;
using d60.EventSorcerer.Views.Basic.Locators;
using MongoDB.Driver;
using NUnit.Framework;

namespace d60.EventSorcerer.Tests.Contracts.Views
{
    [TestFixture(typeof(MongoDbViewManagerFactory), Category = TestCategories.MongoDb)]
    [TestFixture(typeof(MsSqlViewManagerFactory), Category = TestCategories.MsSql)]
    [TestFixture(typeof(EntityFrameworkViewManagerFactory), Category = TestCategories.MsSql)]
    public class ViewLocators<TViewManagerFactory> : FixtureBase where TViewManagerFactory : IViewManagerFactory, new()
    {
        MongoDatabase _database;
        MongoDbEventStore _eventStore;

        IViewManager _globalInstanceViewManager;
        TViewManagerFactory _factory;
        IViewManager _instancePerAggregateRootViewManager;

        protected override void DoSetUp()
        {
            _database = Helper.InitializeTestDatabase();
            _eventStore = new MongoDbEventStore(_database, "events");

            _factory = new TViewManagerFactory();

            _globalInstanceViewManager = _factory.GetViewManagerFor<GlobalInstanceView>();
            _instancePerAggregateRootViewManager = _factory.GetViewManagerFor<InstancePerAggregateRootView>();
        }


        [Test]
        public void WorksWithInstancePerAggregateRootView()
        {
            var rootId1 = Guid.NewGuid();
            var rootId2 = Guid.NewGuid();

            var lastEventForRoot1 = EventFor(rootId1, 2);
            var lastEventForRoot2 = EventFor(rootId2, 2);

            _eventStore.Save(Guid.NewGuid(), new[] { EventFor(rootId1, 0) });
            _eventStore.Save(Guid.NewGuid(), new[] { EventFor(rootId1, 1) });
            _eventStore.Save(Guid.NewGuid(), new[] { lastEventForRoot1 });

            _eventStore.Save(Guid.NewGuid(), new[] { EventFor(rootId2, 0) });
            _eventStore.Save(Guid.NewGuid(), new[] { EventFor(rootId2, 1) });
            _eventStore.Save(Guid.NewGuid(), new[] { lastEventForRoot2 });

            // deliberately dispatch an out-of-sequence event
            _instancePerAggregateRootViewManager.Dispatch(new ThrowingViewContext(), _eventStore, new[] { lastEventForRoot1 });
            _instancePerAggregateRootViewManager.Dispatch(new ThrowingViewContext(), _eventStore, new[] { lastEventForRoot2 });

            var view = _factory.Load<InstancePerAggregateRootView>(InstancePerAggregateRootLocator.GetViewIdFromGuid(rootId1));
            Assert.That(view.EventCounter, Is.EqualTo(3));
        }

        [Test]
        public void WorksWithGlobalInstanceView()
        {
            var rootId1 = Guid.NewGuid();
            var rootId2 = Guid.NewGuid();

            var lastEventForRoot1 = EventFor(rootId1, 2);
            var lastEventForRoot2 = EventFor(rootId2, 2);

            _eventStore.Save(Guid.NewGuid(), new[] { EventFor(rootId1, 0) });
            _eventStore.Save(Guid.NewGuid(), new[] { EventFor(rootId1, 1) });
            _eventStore.Save(Guid.NewGuid(), new[] { lastEventForRoot1 });

            _eventStore.Save(Guid.NewGuid(), new[] { EventFor(rootId2, 0) });
            _eventStore.Save(Guid.NewGuid(), new[] { EventFor(rootId2, 1) });
            _eventStore.Save(Guid.NewGuid(), new[] { lastEventForRoot2 });

            // deliberately dispatch an out-of-sequence event
            _globalInstanceViewManager.Dispatch(new ThrowingViewContext(), _eventStore, new[] { lastEventForRoot1 });
            _globalInstanceViewManager.Dispatch(new ThrowingViewContext(), _eventStore, new[] { lastEventForRoot2 });

            var view = _factory.Load<GlobalInstanceView>(GlobalInstanceLocator.GetViewInstanceId());
            Assert.That(view.EventCounter, Is.EqualTo(6));
        }

        DomainEvent EventFor(Guid aggregateRootId, int seqNo)
        {
            return new AnEvent
            {
                Meta =
                {
                    { DomainEvent.MetadataKeys.AggregateRootId, aggregateRootId },
                    { DomainEvent.MetadataKeys.SequenceNumber, seqNo },
                }
            };
        }

        class GlobalInstanceView : IView<GlobalInstanceLocator>, ISubscribeTo<AnEvent>
        {
            public int EventCounter { get; set; }
            public void Handle(IViewContext context, AnEvent domainEvent)
            {
                EventCounter++;
            }

            public string Id { get; set; }
        }
        class InstancePerAggregateRootView : IView<InstancePerAggregateRootLocator>, ISubscribeTo<AnEvent>
        {
            public int EventCounter { get; set; }
            public void Handle(IViewContext context, AnEvent domainEvent)
            {
                EventCounter++;
            }

            public string Id { get; set; }
        }

        class AnEvent : DomainEvent
        {

        }
    }
}
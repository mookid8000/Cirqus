using System;
using d60.EventSorcerer.Events;
using d60.EventSorcerer.MongoDb.Events;
using d60.EventSorcerer.MongoDb.Views;
using d60.EventSorcerer.Views.Basic;
using d60.EventSorcerer.Views.Basic.Locators;
using MongoDB.Driver;
using NUnit.Framework;

namespace d60.EventSorcerer.Tests.MongoDb.Views
{
    [TestFixture]
    [Category(TestCategories.MongoDb)]
    public class TestMongoDbCatchUpViewManager_GlobalViewLocator : FixtureBase
    {
        MongoDatabase _database;
        MongoDbCatchUpViewManager<JustAnotherView> _viewManager;
        MongoDbEventStore _eventStore;

        protected override void DoSetUp()
        {
            _database = Helper.InitializeTestDatabase();
            _eventStore = new MongoDbEventStore(_database, "events");
            _viewManager = new MongoDbCatchUpViewManager<JustAnotherView>(_database, "justAnother");
        }


        [Test]
        public void CanCatchUpIfEventStoreAllowsIt()
        {
            var rootId1 = Guid.NewGuid();
            var rootId2 = Guid.NewGuid();

            var lastEventForRoot1 = EventFor(rootId1, 2);
            var lastEventForRoot2 = EventFor(rootId2, 2);

            _eventStore.Save(Guid.NewGuid(), new[] { EventFor(rootId1, 0) });
            _eventStore.Save(Guid.NewGuid(), new[] {EventFor(rootId1, 1)});
            _eventStore.Save(Guid.NewGuid(), new[] {lastEventForRoot1});
            
            _eventStore.Save(Guid.NewGuid(), new[] {EventFor(rootId2, 0)});
            _eventStore.Save(Guid.NewGuid(), new[] {EventFor(rootId2, 1)});
            _eventStore.Save(Guid.NewGuid(), new[] {lastEventForRoot2});

            // deliberately dispatch an out-of-sequence event
            _viewManager.Dispatch(_eventStore, new[] {lastEventForRoot1});
            _viewManager.Dispatch(_eventStore, new[] {lastEventForRoot2});

            var view = _viewManager.Load(GlobalInstanceLocator.GetViewInstanceId());
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

        class JustAnotherView : IView<GlobalInstanceLocator>, ISubscribeTo<AnEvent>
        {
            public int EventCounter { get; set; }
            public void Handle(AnEvent domainEvent)
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
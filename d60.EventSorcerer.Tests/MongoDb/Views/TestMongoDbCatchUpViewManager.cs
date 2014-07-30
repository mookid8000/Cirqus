using System;
using d60.EventSorcerer.Events;
using d60.EventSorcerer.Exceptions;
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
    public class TestMongoDbCatchUpViewManager : FixtureBase
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
        public void CanGenerateViewFromNewEvents()
        {
            var rootId1 = Guid.NewGuid();
            var rootId2 = Guid.NewGuid();

            _viewManager.Dispatch(_eventStore, new[]
            {
                EventFor(rootId1, 1),
                EventFor(rootId1, 2),
                EventFor(rootId1, 3),
                EventFor(rootId2, 1),
            });

            var firstView = _viewManager.Load(InstancePerAggregateRootLocator.GetViewIdFromGuid(rootId1));
            Assert.That(firstView.EventCounter, Is.EqualTo(3));

            var secondView = _viewManager.Load(InstancePerAggregateRootLocator.GetViewIdFromGuid(rootId2));
            Assert.That(secondView.EventCounter, Is.EqualTo(1));
        }

        [Test]
        public void RejectsOutOfSequenceEvents()
        {
            var rootId1 = Guid.NewGuid();

            _viewManager.Dispatch(_eventStore, new[] { EventFor(rootId1, 1) });
            _viewManager.Dispatch(_eventStore, new[] { EventFor(rootId1, 2) });

            Assert.Throws<ConsistencyException>(() => _viewManager.Dispatch(_eventStore, new[] { EventFor(rootId1, 1) }));
            Assert.Throws<ConsistencyException>(() => _viewManager.Dispatch(_eventStore, new[] { EventFor(rootId1, 2) }));
            Assert.Throws<ConsistencyException>(() => _viewManager.Dispatch(_eventStore, new[] { EventFor(rootId1, 4) }));
        }

        [Test]
        public void RejectsOutOfSequenceEventsWithCounterPerAggregateRoot()
        {
            var rootId1 = Guid.NewGuid();
            var rootId2 = Guid.NewGuid();

            _viewManager.Dispatch(_eventStore, new[] { EventFor(rootId1, 1) });
            _viewManager.Dispatch(_eventStore, new[] { EventFor(rootId1, 2) });
            _viewManager.Dispatch(_eventStore, new[] { EventFor(rootId1, 3) });
            
            _viewManager.Dispatch(_eventStore, new[] { EventFor(rootId2, 1) });
            _viewManager.Dispatch(_eventStore, new[] { EventFor(rootId2, 2) });

            Assert.Throws<ConsistencyException>(() => _viewManager.Dispatch(_eventStore, new[] { EventFor(rootId1, 5) }));
            Assert.Throws<ConsistencyException>(() => _viewManager.Dispatch(_eventStore, new[] { EventFor(rootId2, 4) }));
        }

        [Test]
        public void CanCatchUpIfEventStoreAllowsIt()
        {
            var rootId1 = Guid.NewGuid();

            _eventStore.Save(Guid.NewGuid(), new[] {EventFor(rootId1, 1)});
            _eventStore.Save(Guid.NewGuid(), new[] {EventFor(rootId1, 2)});
            _eventStore.Save(Guid.NewGuid(), new[] {EventFor(rootId1, 3)});

            _viewManager.Dispatch(_eventStore, new[] { EventFor(rootId1, 1) });
            // deliberately dispatch an out-of-sequence event
            _viewManager.Dispatch(_eventStore, new[] {EventFor(rootId1, 3)});

            var view = _viewManager.Load(InstancePerAggregateRootLocator.GetViewIdFromGuid(rootId1));
            Assert.That(view.EventCounter, Is.EqualTo(3));
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

        class JustAnotherView : IView<InstancePerAggregateRootLocator>, ISubscribeTo<AnEvent>
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
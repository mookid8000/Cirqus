using System;
using System.Linq;
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
        MongoDbCatchUpViewManager<JustAnotherView> _justAnotherViewViewManager;
        MongoDbCatchUpViewManager<ViewThatCanThrow> _viewThatCanThrowViewManager;
        MongoDbEventStore _eventStore;

        protected override void DoSetUp()
        {
            _database = Helper.InitializeTestDatabase();
            _eventStore = new MongoDbEventStore(_database, "events");
            _justAnotherViewViewManager = new MongoDbCatchUpViewManager<JustAnotherView>(_database, "justAnother");
            _viewThatCanThrowViewManager = new MongoDbCatchUpViewManager<ViewThatCanThrow>(_database, "errorTest");
        }

        [Test]
        public void IgnoresEventsWhenTheyHaveAlreadyBeenProcessed()
        {
            // arrange
            ViewThatCanThrow.ThrowAfterThisManyEvents = int.MaxValue;
            var rootId1 = Guid.NewGuid();
            var domainEvents = new[]
            {
                EventFor(rootId1, 0, 0),
                EventFor(rootId1, 1, 1),
                EventFor(rootId1, 2, 2),
            };

            _viewThatCanThrowViewManager.Dispatch(_eventStore, domainEvents);

            // act
            _viewThatCanThrowViewManager.Dispatch(_eventStore, domainEvents);

            // assert
            var view = _viewThatCanThrowViewManager.Load(InstancePerAggregateRootLocator.GetViewIdFromGuid(rootId1));
            
            Assert.That(view.EventsHandled, Is.EqualTo(3));
        }

        [Test]
        public void IgnoresEventsWhenTheyHaveAlreadyBeenProcessed_EnsureTailIsStillProcessed()
        {
            // arrange
            ViewThatCanThrow.ThrowAfterThisManyEvents = int.MaxValue;
            var rootId1 = Guid.NewGuid();
            var domainEvents = new[]
            {
                EventFor(rootId1, 0, 0),
                EventFor(rootId1, 1, 1),
                EventFor(rootId1, 2, 2),
            };

            _viewThatCanThrowViewManager.Dispatch(_eventStore, domainEvents);

            // act
            var domainEventsWithOneAdditionalEvent = domainEvents
                .Concat(new[] {EventFor(rootId1, 3, 3)});

            _viewThatCanThrowViewManager.Dispatch(_eventStore, domainEventsWithOneAdditionalEvent);

            // assert
            var view = _viewThatCanThrowViewManager.Load(InstancePerAggregateRootLocator.GetViewIdFromGuid(rootId1));
            
            Assert.That(view.EventsHandled, Is.EqualTo(4));
        }


        [Test]
        public void CorrectlyHaltsEventDispatchToViewInCaseOfError()
        {
            // arrange
            var rootId1 = Guid.NewGuid();
            _eventStore.Save(Guid.NewGuid(), new[] {EventFor(rootId1, 0)});
            _eventStore.Save(Guid.NewGuid(), new[] {EventFor(rootId1, 1)});
            _eventStore.Save(Guid.NewGuid(), new[] {EventFor(rootId1, 2)});
            _eventStore.Save(Guid.NewGuid(), new[] {EventFor(rootId1, 3)});
            _eventStore.Save(Guid.NewGuid(), new[] {EventFor(rootId1, 4)});

            _viewThatCanThrowViewManager.MaxDomainEventsBetweenFlush = 1;
            ViewThatCanThrow.ThrowAfterThisManyEvents = 3;

            // act
            _viewThatCanThrowViewManager.Initialize(_eventStore);

            // assert
            var view = _viewThatCanThrowViewManager.Load(InstancePerAggregateRootLocator.GetViewIdFromGuid(rootId1));
            Assert.That(view.EventsHandled, Is.EqualTo(2));
        }

        [Test]
        public void CorrectlyHaltsAndResumesEventDispatchToViewInCaseOfError()
        {
            // arrange
            var rootId1 = Guid.NewGuid();
            _eventStore.Save(Guid.NewGuid(), new[] {EventFor(rootId1, 0)});
            _eventStore.Save(Guid.NewGuid(), new[] {EventFor(rootId1, 1)});
            _eventStore.Save(Guid.NewGuid(), new[] {EventFor(rootId1, 2)});
            _eventStore.Save(Guid.NewGuid(), new[] {EventFor(rootId1, 3)});
            _eventStore.Save(Guid.NewGuid(), new[] {EventFor(rootId1, 4)});

            _viewThatCanThrowViewManager.MaxDomainEventsBetweenFlush = 1;
            ViewThatCanThrow.ThrowAfterThisManyEvents = 3;
            _viewThatCanThrowViewManager.Initialize(_eventStore);

            ViewThatCanThrow.ThrowAfterThisManyEvents = int.MaxValue;

            // act
            _viewThatCanThrowViewManager.Initialize(_eventStore);

            // assert
            var view = _viewThatCanThrowViewManager.Load(InstancePerAggregateRootLocator.GetViewIdFromGuid(rootId1));
            Assert.That(view.EventsHandled, Is.EqualTo(5));
        }

        [Test]
        public void FlushesAfterEachEventAfterEventDispatchHaltsTheFirstTime()
        {
            // arrange
            _viewThatCanThrowViewManager.MaxDomainEventsBetweenFlush = 10;

            var rootId1 = Guid.NewGuid();
            _eventStore.Save(Guid.NewGuid(), new[] {EventFor(rootId1, 0)});
            _eventStore.Save(Guid.NewGuid(), new[] {EventFor(rootId1, 1)});
            _eventStore.Save(Guid.NewGuid(), new[] {EventFor(rootId1, 2)});
            _eventStore.Save(Guid.NewGuid(), new[] {EventFor(rootId1, 3)});
            _eventStore.Save(Guid.NewGuid(), new[] {EventFor(rootId1, 4)});

            ViewThatCanThrow.ThrowAfterThisManyEvents = 3;

            // act
            _viewThatCanThrowViewManager.Initialize(_eventStore);

            // assert
            var view = _viewThatCanThrowViewManager.Load(InstancePerAggregateRootLocator.GetViewIdFromGuid(rootId1));
            Assert.That(view.EventsHandled, Is.EqualTo(2));
        }

        class ViewThatCanThrow : IView<InstancePerAggregateRootLocator>, ISubscribeTo<AnEvent>
        {
            public static  int ThrowAfterThisManyEvents { get; set; }
            public string Id { get; set; }
            public int EventsHandled { get; set; }
            public void Handle(AnEvent domainEvent)
            {
                EventsHandled++;

                if (EventsHandled >= ThrowAfterThisManyEvents)
                {
                    throw new Exception("w00tadafook!?");
                }
            }
        }

        [Test]
        public void CanGenerateViewFromNewEvents()
        {
            var rootId1 = Guid.NewGuid();
            var rootId2 = Guid.NewGuid();

            _justAnotherViewViewManager.Dispatch(_eventStore, new[]
            {
                EventFor(rootId1, 0),
                EventFor(rootId1, 1),
                EventFor(rootId1, 2),
                EventFor(rootId2, 0),
            });

            var firstView = _justAnotherViewViewManager.Load(InstancePerAggregateRootLocator.GetViewIdFromGuid(rootId1));
            Assert.That(firstView.EventCounter, Is.EqualTo(3));

            var secondView = _justAnotherViewViewManager.Load(InstancePerAggregateRootLocator.GetViewIdFromGuid(rootId2));
            Assert.That(secondView.EventCounter, Is.EqualTo(1));
        }

        [Test]
        public void RejectsOutOfSequenceEvents()
        {
            var rootId1 = Guid.NewGuid();

            var firstEvent = EventFor(rootId1, 0);
            var nextEvent = EventFor(rootId1, 1);

            _justAnotherViewViewManager.Dispatch(_eventStore, new[] { firstEvent });
            _justAnotherViewViewManager.Dispatch(_eventStore, new[] { nextEvent });

            Assert.Throws<ConsistencyException>(() => _justAnotherViewViewManager.Dispatch(_eventStore, new[] { firstEvent }));
            Assert.Throws<ConsistencyException>(() => _justAnotherViewViewManager.Dispatch(_eventStore, new[] { nextEvent }));
            Assert.Throws<ConsistencyException>(() => _justAnotherViewViewManager.Dispatch(_eventStore, new[] { EventFor(rootId1, 3) }));
            Assert.Throws<ConsistencyException>(() => _justAnotherViewViewManager.Dispatch(_eventStore, new[] { EventFor(rootId1, 4) }));
        }

        [Test]
        public void RejectsOutOfSequenceEventsWithCounterPerAggregateRoot()
        {
            var rootId1 = Guid.NewGuid();
            var rootId2 = Guid.NewGuid();

            _justAnotherViewViewManager.Dispatch(_eventStore, new[] { EventFor(rootId1, 0) });
            _justAnotherViewViewManager.Dispatch(_eventStore, new[] { EventFor(rootId1, 1) });
            _justAnotherViewViewManager.Dispatch(_eventStore, new[] { EventFor(rootId1, 2) });

            _justAnotherViewViewManager.Dispatch(_eventStore, new[] { EventFor(rootId2, 0) });
            _justAnotherViewViewManager.Dispatch(_eventStore, new[] { EventFor(rootId2, 1) });

            Assert.Throws<ConsistencyException>(() => _justAnotherViewViewManager.Dispatch(_eventStore, new[] { EventFor(rootId1, 4) }));
            Assert.Throws<ConsistencyException>(() => _justAnotherViewViewManager.Dispatch(_eventStore, new[] { EventFor(rootId2, 3) }));
        }

        [Test]
        public void CanCatchUpIfEventStoreAllowsIt()
        {
            var rootId1 = Guid.NewGuid();

            var firstEvent = EventFor(rootId1, 0);
            var lastEvent = EventFor(rootId1, 2);

            _eventStore.Save(Guid.NewGuid(), new[] { firstEvent });
            _eventStore.Save(Guid.NewGuid(), new[] { EventFor(rootId1, 1) });
            _eventStore.Save(Guid.NewGuid(), new[] { lastEvent });

            _justAnotherViewViewManager.Dispatch(_eventStore, new[] { firstEvent });
            // deliberately dispatch an out-of-sequence event
            _justAnotherViewViewManager.Dispatch(_eventStore, new[] { lastEvent });

            var view = _justAnotherViewViewManager.Load(InstancePerAggregateRootLocator.GetViewIdFromGuid(rootId1));
            Assert.That(view.EventCounter, Is.EqualTo(3));
        }

        DomainEvent EventFor(Guid aggregateRootId, int seqNo, int globalSeqNo = -1)
        {
            var e = new AnEvent
            {
                Meta =
                {
                    { DomainEvent.MetadataKeys.AggregateRootId, aggregateRootId },
                    { DomainEvent.MetadataKeys.SequenceNumber, seqNo },
                }
            };

            if (globalSeqNo >= 0)
            {
                e.Meta[DomainEvent.MetadataKeys.GlobalSequenceNumber] = globalSeqNo;
            }

            return e;
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
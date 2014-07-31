using System;
using System.Linq;
using d60.EventSorcerer.Events;
using d60.EventSorcerer.Exceptions;
using d60.EventSorcerer.MongoDb.Events;
using d60.EventSorcerer.Tests.Contracts.Views.Factories;
using d60.EventSorcerer.Tests.MongoDb;
using d60.EventSorcerer.Views.Basic;
using d60.EventSorcerer.Views.Basic.Locators;
using MongoDB.Driver;
using NUnit.Framework;

namespace d60.EventSorcerer.Tests.Contracts.Views
{
    [TestFixture(typeof(MongoDbCatchUpViewManagerFactory), Category = TestCategories.MongoDb)]
    public class CatchUpViews<TViewManagerFactory> : FixtureBase where TViewManagerFactory : ICatchUpViewManagerFactory, new()
    {
        MongoDatabase _database;
        MongoDbEventStore _eventStore;

        IViewManager _justAnotherViewViewManager;
        IViewManager _viewThatCanThrowViewManager;
        TViewManagerFactory _factory;

        protected override void DoSetUp()
        {
            _database = Helper.InitializeTestDatabase();
            _eventStore = new MongoDbEventStore(_database, "events");

            _factory = new TViewManagerFactory();

            _justAnotherViewViewManager = _factory.GetViewManagerFor<JustAnotherView>();
            _viewThatCanThrowViewManager = _factory.GetViewManagerFor<ViewThatCanThrow>();
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
            var view = _factory.Load<ViewThatCanThrow>(InstancePerAggregateRootLocator.GetViewIdFromGuid(rootId1));

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
                .Concat(new[] { EventFor(rootId1, 3, 3) });

            _viewThatCanThrowViewManager.Dispatch(_eventStore, domainEventsWithOneAdditionalEvent);

            // assert
            var view = _factory.Load<ViewThatCanThrow>(InstancePerAggregateRootLocator.GetViewIdFromGuid(rootId1));

            Assert.That(view.EventsHandled, Is.EqualTo(4));
        }


        [Test]
        public void CorrectlyHaltsEventDispatchToViewInCaseOfError()
        {
            // arrange
            var rootId1 = Guid.NewGuid();
            _eventStore.Save(Guid.NewGuid(), new[] { EventFor(rootId1, 0, 10) });
            _eventStore.Save(Guid.NewGuid(), new[] { EventFor(rootId1, 1, 11) });
            _eventStore.Save(Guid.NewGuid(), new[] { EventFor(rootId1, 2, 12) });
            _eventStore.Save(Guid.NewGuid(), new[] { EventFor(rootId1, 3,13) });
            _eventStore.Save(Guid.NewGuid(), new[] { EventFor(rootId1, 4,14) });

            _factory.SetMaxDomainEventsBetweenFlush(1);
            ViewThatCanThrow.ThrowAfterThisManyEvents = 3;

            // act
            _viewThatCanThrowViewManager.Initialize(_eventStore);

            // assert
            var view = _factory.Load<ViewThatCanThrow>(InstancePerAggregateRootLocator.GetViewIdFromGuid(rootId1));
            Assert.That(view.EventsHandled, Is.EqualTo(2));
        }

        [Test]
        public void CorrectlyHaltsAndResumesEventDispatchToViewInCaseOfError()
        {
            // arrange
            var rootId1 = Guid.NewGuid();
            _eventStore.Save(Guid.NewGuid(), new[] { EventFor(rootId1, 0, 80) });
            _eventStore.Save(Guid.NewGuid(), new[] { EventFor(rootId1, 1,81) });
            _eventStore.Save(Guid.NewGuid(), new[] { EventFor(rootId1, 2,82) });
            _eventStore.Save(Guid.NewGuid(), new[] { EventFor(rootId1, 3,83) });
            _eventStore.Save(Guid.NewGuid(), new[] { EventFor(rootId1, 4,84) });

            _factory.SetMaxDomainEventsBetweenFlush(1);
            ViewThatCanThrow.ThrowAfterThisManyEvents = 3;
            _viewThatCanThrowViewManager.Initialize(_eventStore);

            ViewThatCanThrow.ThrowAfterThisManyEvents = int.MaxValue;

            // act
            _viewThatCanThrowViewManager.Initialize(_eventStore);

            // assert
            var view = _factory.Load<ViewThatCanThrow>(InstancePerAggregateRootLocator.GetViewIdFromGuid(rootId1));
            Assert.That(view.EventsHandled, Is.EqualTo(5));
        }

        [Test]
        public void FlushesAfterEachEventAfterEventDispatchHaltsTheFirstTime()
        {
            // arrange
            _factory.SetMaxDomainEventsBetweenFlush(10);

            var rootId1 = Guid.NewGuid();
            _eventStore.Save(Guid.NewGuid(), new[] { EventFor(rootId1, 0,50) });
            _eventStore.Save(Guid.NewGuid(), new[] { EventFor(rootId1, 1,51) });
            _eventStore.Save(Guid.NewGuid(), new[] { EventFor(rootId1, 2,52) });
            _eventStore.Save(Guid.NewGuid(), new[] { EventFor(rootId1, 3,53) });
            _eventStore.Save(Guid.NewGuid(), new[] { EventFor(rootId1, 4,54) });

            ViewThatCanThrow.ThrowAfterThisManyEvents = 3;

            // act
            _viewThatCanThrowViewManager.Initialize(_eventStore);

            // assert
            var view = _factory.Load<ViewThatCanThrow>(InstancePerAggregateRootLocator.GetViewIdFromGuid(rootId1));
            Assert.That(view.EventsHandled, Is.EqualTo(2));
        }

        class ViewThatCanThrow : IView<InstancePerAggregateRootLocator>, ISubscribeTo<AnEvent>
        {
            public static int ThrowAfterThisManyEvents { get; set; }
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
                EventFor(rootId1, 0, 10),
                EventFor(rootId1, 1, 11),
                EventFor(rootId1, 2, 12),
                EventFor(rootId2, 0, 13),
            });

            var firstView = _factory.Load<JustAnotherView>(InstancePerAggregateRootLocator.GetViewIdFromGuid(rootId1));
            Assert.That(firstView.EventCounter, Is.EqualTo(3));

            var secondView = _factory.Load<JustAnotherView>(InstancePerAggregateRootLocator.GetViewIdFromGuid(rootId2));
            Assert.That(secondView.EventCounter, Is.EqualTo(1));
        }

        [Test]
        public void RejectsOutOfSequenceEvents()
        {
            var rootId1 = Guid.NewGuid();

            var firstEvent = EventFor(rootId1, 0, 1);
            var nextEvent = EventFor(rootId1, 1, 2);

            _justAnotherViewViewManager.Dispatch(_eventStore, new[] { firstEvent });
            _justAnotherViewViewManager.Dispatch(_eventStore, new[] { nextEvent });

            Assert.Throws<ConsistencyException>(() => _justAnotherViewViewManager.Dispatch(_eventStore, new[] { EventFor(rootId1, 3, 3) }));
            Assert.Throws<ConsistencyException>(() => _justAnotherViewViewManager.Dispatch(_eventStore, new[] { EventFor(rootId1, 4, 4) }));
        }

        [Test]
        public void RejectsOutOfSequenceEventsWithCounterPerAggregateRoot()
        {
            var rootId1 = Guid.NewGuid();
            var rootId2 = Guid.NewGuid();

            _justAnotherViewViewManager.Dispatch(_eventStore, new[] { EventFor(rootId1, 0, 10) });
            _justAnotherViewViewManager.Dispatch(_eventStore, new[] { EventFor(rootId1, 1, 11) });
            _justAnotherViewViewManager.Dispatch(_eventStore, new[] { EventFor(rootId1, 2, 12) });

            _justAnotherViewViewManager.Dispatch(_eventStore, new[] { EventFor(rootId2, 0, 13) });
            _justAnotherViewViewManager.Dispatch(_eventStore, new[] { EventFor(rootId2, 1, 14) });

            Assert.Throws<ConsistencyException>(() => _justAnotherViewViewManager.Dispatch(_eventStore, new[] { EventFor(rootId1, 4, 15) }));
            Assert.Throws<ConsistencyException>(() => _justAnotherViewViewManager.Dispatch(_eventStore, new[] { EventFor(rootId2, 3, 16) }));
        }

        [Test]
        public void CanCatchUpIfEventStoreAllowsIt()
        {
            var rootId1 = Guid.NewGuid();

            var firstEvent = EventFor(rootId1, 0, 10);
            var lastEvent = EventFor(rootId1, 2, 12);

            _eventStore.Save(Guid.NewGuid(), new[] { firstEvent });
            _eventStore.Save(Guid.NewGuid(), new[] { EventFor(rootId1, 1, 11) });
            _eventStore.Save(Guid.NewGuid(), new[] { lastEvent });

            _justAnotherViewViewManager.Dispatch(_eventStore, new[] { firstEvent });
            // deliberately dispatch an out-of-sequence event
            _justAnotherViewViewManager.Dispatch(_eventStore, new[] { lastEvent });

            var view = _factory.Load<JustAnotherView>(InstancePerAggregateRootLocator.GetViewIdFromGuid(rootId1));
            Assert.That(view.EventCounter, Is.EqualTo(3));
        }

        DomainEvent EventFor(Guid aggregateRootId, int seqNo, int globalSeqNo)
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
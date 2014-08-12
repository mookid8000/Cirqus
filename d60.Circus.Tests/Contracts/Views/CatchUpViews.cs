using System;
using System.Collections.Generic;
using System.Linq;
using d60.Circus.Aggregates;
using d60.Circus.Events;
using d60.Circus.MongoDb.Events;
using d60.Circus.Tests.Contracts.Views.Factories;
using d60.Circus.Tests.MongoDb;
using d60.Circus.Views.ViewManagers;
using d60.Circus.Views.ViewManagers.Locators;
using MongoDB.Driver;
using NUnit.Framework;

namespace d60.Circus.Tests.Contracts.Views
{
    [TestFixture(typeof(MongoDbPullViewManagerFactory), Category = TestCategories.MongoDb)]
    [TestFixture(typeof(MsSqlPullViewManagerFactory), Category = TestCategories.MsSql)]
    [TestFixture(typeof(EntityFrameworkPullViewManagerFactory), Category = TestCategories.MsSql)]
    public class CatchUpViews<TViewManagerFactory> : FixtureBase where TViewManagerFactory : IPullViewManagerFactory, new()
    {
        MongoDatabase _database;
        MongoDbEventStore _eventStore;

        IViewManager _justAnotherViewViewManager;
        IViewManager _viewThatCanThrowViewManager;

        ViewManagerEventDispatcher _eventDispatcher;

        TViewManagerFactory _factory;

        protected override void DoSetUp()
        {
            _database = MongoHelper.InitializeTestDatabase();
            _eventStore = new MongoDbEventStore(_database, "events");

            _factory = new TViewManagerFactory();

            _justAnotherViewViewManager = _factory.GetPullViewManager<JustAnotherViewInstanceOther>();
            _viewThatCanThrowViewManager = _factory.GetPullViewManager<ViewInstanceThatCanThrow>();

            _eventDispatcher = new ViewManagerEventDispatcher(new DefaultAggregateRootRepository(_eventStore), _justAnotherViewViewManager, _viewThatCanThrowViewManager);
        }

        [Test]
        public void IgnoresEventsWhenTheyHaveAlreadyBeenProcessed()
        {
            // arrange
            ViewInstanceThatCanThrow.ThrowAfterThisManyEvents = int.MaxValue;
            var rootId1 = Guid.NewGuid();
            var domainEvents = new[]
            {
                EventFor(rootId1, 0, 0),
                EventFor(rootId1, 1, 1),
                EventFor(rootId1, 2, 2),
            };

            SaveAndDispatch(domainEvents);

            // act
            SaveAndDispatch(domainEvents, saveEventsToEventStore: false);

            // assert
            var view = _factory.Load<ViewInstanceThatCanThrow>(InstancePerAggregateRootLocator.GetViewIdFromGuid(rootId1));

            Assert.That(view.EventsHandled, Is.EqualTo(3));
        }

        [Test]
        public void IgnoresEventsWhenTheyHaveAlreadyBeenProcessed_EnsureTailIsStillProcessed()
        {
            // arrange
            ViewInstanceThatCanThrow.ThrowAfterThisManyEvents = int.MaxValue;
            var rootId1 = Guid.NewGuid();
            var domainEvents = new[]
            {
                EventFor(rootId1, 0, 0),
                EventFor(rootId1, 1, 1),
                EventFor(rootId1, 2, 2),
            };

            SaveAndDispatch(domainEvents);

            // act
            var additionalEvent = EventFor(rootId1, 3, 3);

            var domainEventsWithOneAdditionalEvent = domainEvents
                .Concat(new[] { additionalEvent });

            //manually save extra event here
            _eventStore.Save(Guid.NewGuid(), new[] { additionalEvent });

            SaveAndDispatch(domainEventsWithOneAdditionalEvent, saveEventsToEventStore: false);

            // assert
            var view = _factory.Load<ViewInstanceThatCanThrow>(InstancePerAggregateRootLocator.GetViewIdFromGuid(rootId1));

            Assert.That(view.EventsHandled, Is.EqualTo(4));
        }


        [Test]
        public void CorrectlyHaltsEventDispatchToViewInCaseOfError_Initialization()
        {
            // arrange
            var rootId1 = Guid.NewGuid();
            _eventStore.Save(Guid.NewGuid(), new[] { EventFor(rootId1, 0, 10) });
            _eventStore.Save(Guid.NewGuid(), new[] { EventFor(rootId1, 1, 11) });
            _eventStore.Save(Guid.NewGuid(), new[] { EventFor(rootId1, 2, 12) });
            _eventStore.Save(Guid.NewGuid(), new[] { EventFor(rootId1, 3, 13) });
            _eventStore.Save(Guid.NewGuid(), new[] { EventFor(rootId1, 4, 14) });

            _factory.SetMaxDomainEventsBetweenFlush(1);
            ViewInstanceThatCanThrow.ThrowAfterThisManyEvents = 3;

            // act
            _eventDispatcher.Initialize(_eventStore);

            // assert
            var view = _factory.Load<ViewInstanceThatCanThrow>(InstancePerAggregateRootLocator.GetViewIdFromGuid(rootId1));
            Assert.That(view.EventsHandled, Is.EqualTo(2));
        }

        [Test]
        public void CorrectlyHaltsEventDispatchToViewInCaseOfError_Dispatch()
        {
            // arrange
            var rootId1 = Guid.NewGuid();

            var events = new[]
            {
                EventFor(rootId1, 0, 10),
                EventFor(rootId1, 1, 11),
                EventFor(rootId1, 2, 12),
                EventFor(rootId1, 3, 13),
                EventFor(rootId1, 4, 14),
            };

            _factory.SetMaxDomainEventsBetweenFlush(1);
            ViewInstanceThatCanThrow.ThrowAfterThisManyEvents = 3;

            // act
            SaveAndDispatch(events);

            // assert
            var view = _factory.Load<ViewInstanceThatCanThrow>(InstancePerAggregateRootLocator.GetViewIdFromGuid(rootId1));
            Assert.That(view.EventsHandled, Is.EqualTo(2));
        }

        [Test]
        public void CorrectlyHaltsAndResumesEventDispatchToViewInCaseOfError()
        {
            // arrange
            var rootId1 = Guid.NewGuid();
            _eventStore.Save(Guid.NewGuid(), new[] { EventFor(rootId1, 0, 80) });
            _eventStore.Save(Guid.NewGuid(), new[] { EventFor(rootId1, 1, 81) });
            _eventStore.Save(Guid.NewGuid(), new[] { EventFor(rootId1, 2, 82) });
            _eventStore.Save(Guid.NewGuid(), new[] { EventFor(rootId1, 3, 83) });
            _eventStore.Save(Guid.NewGuid(), new[] { EventFor(rootId1, 4, 84) });

            _factory.SetMaxDomainEventsBetweenFlush(1);
            ViewInstanceThatCanThrow.ThrowAfterThisManyEvents = 3;
            _eventDispatcher.Initialize(_eventStore);

            // don't throw anymore
            ViewInstanceThatCanThrow.ThrowAfterThisManyEvents = int.MaxValue;

            // act
            _eventDispatcher.Initialize(_eventStore);

            // assert
            var view = _factory.Load<ViewInstanceThatCanThrow>(InstancePerAggregateRootLocator.GetViewIdFromGuid(rootId1));

            Assert.That(view.EventsHandled, Is.EqualTo(5));
        }

        [Test]
        public void FlushesAfterEachEventAfterEventDispatchHaltsTheFirstTime()
        {
            // arrange
            _factory.SetMaxDomainEventsBetweenFlush(10);

            var rootId1 = Guid.NewGuid();
            _eventStore.Save(Guid.NewGuid(), new[] { EventFor(rootId1, 0, 50) });
            _eventStore.Save(Guid.NewGuid(), new[] { EventFor(rootId1, 1, 51) });
            _eventStore.Save(Guid.NewGuid(), new[] { EventFor(rootId1, 2, 52) });
            _eventStore.Save(Guid.NewGuid(), new[] { EventFor(rootId1, 3, 53) });
            _eventStore.Save(Guid.NewGuid(), new[] { EventFor(rootId1, 4, 54) });

            ViewInstanceThatCanThrow.ThrowAfterThisManyEvents = 3;

            // act
            _eventDispatcher.Initialize(_eventStore);

            // assert
            var view = _factory.Load<ViewInstanceThatCanThrow>(InstancePerAggregateRootLocator.GetViewIdFromGuid(rootId1));
            Assert.That(view.EventsHandled, Is.EqualTo(2));
        }

        [Test]
        public void CanGenerateViewFromNewEvents()
        {
            var rootId1 = Guid.NewGuid();
            var rootId2 = Guid.NewGuid();

            var events = new[]
            {
                EventFor(rootId1, 0, 10),
                EventFor(rootId1, 1, 11),
                EventFor(rootId1, 2, 12),
                EventFor(rootId2, 0, 13),
            };

            SaveAndDispatch(events);

            var firstView = _factory.Load<JustAnotherViewInstanceOther>(InstancePerAggregateRootLocator.GetViewIdFromGuid(rootId1));
            Assert.That(firstView.EventCounter, Is.EqualTo(3));

            var secondView = _factory.Load<JustAnotherViewInstanceOther>(InstancePerAggregateRootLocator.GetViewIdFromGuid(rootId2));
            Assert.That(secondView.EventCounter, Is.EqualTo(1));
        }

        void SaveAndDispatch(IEnumerable<DomainEvent> domainEvents, bool saveEventsToEventStore = true)
        {
            var list = domainEvents.ToList();

            if (saveEventsToEventStore)
            {
                _eventStore.Save(Guid.NewGuid(), list);
            }

            _eventDispatcher.Dispatch(_eventStore, list);
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
    }

    class ViewInstanceThatCanThrow : IViewInstance<InstancePerAggregateRootLocator>, ISubscribeTo<AnEvent>
    {
        public ViewInstanceThatCanThrow()
        {
            JustSomeString = "needs to be set to something";
        }
        public static int ThrowAfterThisManyEvents { get; set; }
        public string Id { get; set; }
        public long LastGlobalSequenceNumber { get; set; }
        public int EventsHandled { get; set; }
        public string JustSomeString { get; set; }
        public decimal Decimal { get; set; }
        public void Handle(IViewContext context, AnEvent domainEvent)
        {
            EventsHandled++;

            if (EventsHandled >= ThrowAfterThisManyEvents)
            {
                throw new Exception("w00tadafook!?");
            }
        }
    }

    class AnEvent : DomainEvent
    {

    }

    class JustAnotherViewInstanceOther : IViewInstance<InstancePerAggregateRootLocator>, ISubscribeTo<AnEvent>
    {
        public int EventCounter { get; set; }
        public void Handle(IViewContext context, AnEvent domainEvent)
        {
            EventCounter++;

            Console.WriteLine("Event counter incremented to {0}", EventCounter);
        }

        public string Id { get; set; }
        public long LastGlobalSequenceNumber { get; set; }
    }
}
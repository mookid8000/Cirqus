using System;
using System.Linq;
using d60.EventSorcerer.Aggregates;
using d60.EventSorcerer.Events;
using d60.EventSorcerer.MongoDb.Events;
using d60.EventSorcerer.Tests.Contracts.Views.Factories;
using d60.EventSorcerer.Tests.MongoDb;
using d60.EventSorcerer.Views.Basic;
using d60.EventSorcerer.Views.Basic.Locators;
using MongoDB.Driver;
using NUnit.Framework;

namespace d60.EventSorcerer.Tests.Contracts.Views
{
    [TestFixture(typeof(MongoDbViewManagerFactory), Category = TestCategories.MongoDb)]
    [TestFixture(typeof(MsSqlViewManagerFactory), Category = TestCategories.MsSql)]
    [TestFixture(typeof(EntityFrameworkViewManagerFactory), Category = TestCategories.MsSql)]
    public class CatchUpViews<TViewManagerFactory> : FixtureBase where TViewManagerFactory : IViewManagerFactory, new()
    {
        MongoDatabase _database;
        MongoDbEventStore _eventStore;

        IViewManager _justAnotherViewViewManager;
        IViewManager _viewThatCanThrowViewManager;

        BasicEventDispatcher _eventDispatcher;

        TViewManagerFactory _factory;

        protected override void DoSetUp()
        {
            _database = MongoHelper.InitializeTestDatabase();
            _eventStore = new MongoDbEventStore(_database, "events");

            _factory = new TViewManagerFactory();

            _justAnotherViewViewManager = _factory.GetViewManagerFor<JustAnotherViewOther>();
            _viewThatCanThrowViewManager = _factory.GetViewManagerFor<ViewThatCanThrow>();

            _eventDispatcher = new BasicEventDispatcher(new BasicAggregateRootRepository(_eventStore), _justAnotherViewViewManager, _viewThatCanThrowViewManager);
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

            _eventDispatcher.Dispatch(_eventStore, domainEvents);

            // act
            _eventDispatcher.Dispatch(_eventStore, domainEvents);

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

            _eventDispatcher.Dispatch(_eventStore, domainEvents);

            // act
            var domainEventsWithOneAdditionalEvent = domainEvents
                .Concat(new[] { EventFor(rootId1, 3, 3) });

            _eventDispatcher.Dispatch(_eventStore, domainEventsWithOneAdditionalEvent);

            // assert
            var view = _factory.Load<ViewThatCanThrow>(InstancePerAggregateRootLocator.GetViewIdFromGuid(rootId1));

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
            ViewThatCanThrow.ThrowAfterThisManyEvents = 3;

            // act
            _eventDispatcher.Initialize(_eventStore);

            // assert
            var view = _factory.Load<ViewThatCanThrow>(InstancePerAggregateRootLocator.GetViewIdFromGuid(rootId1));
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
            ViewThatCanThrow.ThrowAfterThisManyEvents = 3;

            // act
            _eventDispatcher.Dispatch(_eventStore, events);

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
            _eventStore.Save(Guid.NewGuid(), new[] { EventFor(rootId1, 1, 81) });
            _eventStore.Save(Guid.NewGuid(), new[] { EventFor(rootId1, 2, 82) });
            _eventStore.Save(Guid.NewGuid(), new[] { EventFor(rootId1, 3, 83) });
            _eventStore.Save(Guid.NewGuid(), new[] { EventFor(rootId1, 4, 84) });

            _factory.SetMaxDomainEventsBetweenFlush(1);
            ViewThatCanThrow.ThrowAfterThisManyEvents = 3;
            _eventDispatcher.Initialize(_eventStore);

            // don't throw anymore
            ViewThatCanThrow.ThrowAfterThisManyEvents = int.MaxValue;

            // act
            _eventDispatcher.Initialize(_eventStore);

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
            _eventStore.Save(Guid.NewGuid(), new[] { EventFor(rootId1, 0, 50) });
            _eventStore.Save(Guid.NewGuid(), new[] { EventFor(rootId1, 1, 51) });
            _eventStore.Save(Guid.NewGuid(), new[] { EventFor(rootId1, 2, 52) });
            _eventStore.Save(Guid.NewGuid(), new[] { EventFor(rootId1, 3, 53) });
            _eventStore.Save(Guid.NewGuid(), new[] { EventFor(rootId1, 4, 54) });

            ViewThatCanThrow.ThrowAfterThisManyEvents = 3;

            // act
            _eventDispatcher.Initialize(_eventStore);

            // assert
            var view = _factory.Load<ViewThatCanThrow>(InstancePerAggregateRootLocator.GetViewIdFromGuid(rootId1));
            Assert.That(view.EventsHandled, Is.EqualTo(2));
        }

        [Test]
        public void CanGenerateViewFromNewEvents()
        {
            var rootId1 = Guid.NewGuid();
            var rootId2 = Guid.NewGuid();

            _eventDispatcher.Dispatch(_eventStore, new[]
            {
                EventFor(rootId1, 0, 10),
                EventFor(rootId1, 1, 11),
                EventFor(rootId1, 2, 12),
                EventFor(rootId2, 0, 13),
            });

            var firstView = _factory.Load<JustAnotherViewOther>(InstancePerAggregateRootLocator.GetViewIdFromGuid(rootId1));
            Assert.That(firstView.EventCounter, Is.EqualTo(3));

            var secondView = _factory.Load<JustAnotherViewOther>(InstancePerAggregateRootLocator.GetViewIdFromGuid(rootId2));
            Assert.That(secondView.EventCounter, Is.EqualTo(1));
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

    class ViewThatCanThrow : IView<InstancePerAggregateRootLocator>, ISubscribeTo<AnEvent>
    {
        public ViewThatCanThrow()
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

    class JustAnotherViewOther : IView<InstancePerAggregateRootLocator>, ISubscribeTo<AnEvent>
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
using System;
using System.Collections.Generic;
using System.Linq;
using d60.Cirqus.Aggregates;
using d60.Cirqus.Events;
using d60.Cirqus.Extensions;
using d60.Cirqus.Numbers;
using d60.Cirqus.Views.ViewManagers;
using NUnit.Framework;
using TestContext = d60.Cirqus.TestHelpers.TestContext;

namespace d60.Cirqus.Tests.TestHelpers
{
    [TestFixture]
    public class TestTestContext : FixtureBase
    {
        TestContext _context;

        protected override void DoSetUp()
        {
            _context = new TestContext();
        }

        [Test]
        public void AlsoPicksUpMetadataFromAggregate()
        {
            // arrange
            // act
            _context.Save(Guid.NewGuid(), new EventForThatRoot());

            // assert
            var thatEvent = _context.History.Cast<EventForThatRoot>().Single();
            Assert.That(thatEvent.Meta["root"], Is.EqualTo("bim"));
            Assert.That(thatEvent.Meta["event"], Is.EqualTo("bom"));
        }

        [Meta("event", "bom")]
        class EventForThatRoot : DomainEvent<RootWithMetadata> { }
        
        [Meta("root", "bim")]
        class RootWithMetadata : AggregateRoot
        {
            
        }


        [Test]
        public void CanDispatchToViews()
        {
            var viewMan = new SillyViewManager();
            _context.AddViewManager(viewMan);
            var aggregateRootId = Guid.NewGuid();

            _context.Save(aggregateRootId, new AnEvent());

            Assert.That(viewMan.ReceivedDomainEvents.Count, Is.EqualTo(1));
        }

        [Test]
        public void UncommittedEventsAreNotDispatchedToViews()
        {
            var viewMan = new SillyViewManager();
            _context.AddViewManager(viewMan);
            var aggregateRootId = Guid.NewGuid();

            _context.Save(aggregateRootId, new AnEvent());

            Assert.That(viewMan.ReceivedDomainEvents.Count, Is.EqualTo(0));
        }

        [Test]
        public void VerifiesThatEventsCanBeSerialized()
        {
            Assert.Throws<ArgumentException >(() => _context.Save(Guid.NewGuid(), UnserializableDomainEvent.Create("hej der!")));
        }

        class UnserializableDomainEvent : DomainEvent<AnAggregate>
        {
            public static UnserializableDomainEvent Create(string text)
            {
                return new UnserializableDomainEvent { MyString = text };
            }

            public string MyString { get; protected set; }
        }


        class SillyViewManager : IPushViewManager
        {
            public SillyViewManager()
            {
                ReceivedDomainEvents = new List<DomainEvent>();
            }
            public List<DomainEvent> ReceivedDomainEvents { get; set; }
            public void Initialize(IViewContext context, IEventStore eventStore, bool purgeExistingViews = false)
            {
                ReceivedDomainEvents.AddRange(eventStore.Stream().ToList());
            }

            public bool Stopped { get; set; }

            public void Dispatch(IViewContext context, IEventStore eventStore, IEnumerable<DomainEvent> events)
            {
                ReceivedDomainEvents.AddRange(events);
            }
        }


        [Test]
        public void HydratesEntitiesWithExistingEvents()
        {
            // arrange
            var rootId = Guid.NewGuid();

            _context.Save(rootId, new AnEvent());
            _context.Save(rootId, new AnEvent());
            _context.Save(rootId, new AnEvent());

            // act
            var firstInstance = _context.BeginUnitOfWork().Get<AnAggregate>(rootId);

            // assert
            Assert.That(firstInstance.ProcessedEvents, Is.EqualTo(3));
        }

        [Test]
        public void EmittedEventsAreCollectedInUnitOfWork()
        {
            // arrange
            var rootId = Guid.NewGuid();
            var uow = _context.BeginUnitOfWork();
            var root = uow.Get<AnAggregate>(rootId);

            // act
            root.DoStuff();

            // assert
            Assert.That(uow.EmittedEvents.Cast<AnEvent>().Single(), Is.TypeOf<AnEvent>());
            Assert.That(uow.EmittedEvents.Cast<AnEvent>().Single().GetAggregateRootId(), Is.EqualTo(rootId));
        }

        [Test]
        public void CommittedEventsBecomeTheHistory()
        {
            // arrange
            var rootId = Guid.NewGuid();
            var uow = _context.BeginUnitOfWork();
            var root = uow.Get<AnAggregate>(rootId);
            root.DoStuff();

            // act
            uow.Commit();

            // assert
            Assert.That(uow.EmittedEvents.Count(), Is.EqualTo(0));
            Assert.That(_context.History.Cast<AnEvent>().Single(), Is.TypeOf<AnEvent>());
            Assert.That(_context.History.Cast<AnEvent>().Single().GetAggregateRootId(), Is.EqualTo(rootId));
        }
    }

    public class AnAggregate : AggregateRoot, IEmit<AnEvent>
    {
        public int ProcessedEvents { get; set; }
        public void Apply(AnEvent e)
        {
            ProcessedEvents++;
        }

        public void DoStuff()
        {
            Emit(new AnEvent());
        }
    }

    public class AnEvent : DomainEvent<AnAggregate>
    {

    }

    public class AnotherAggregate : AggregateRoot, IEmit<AnotherEvent>
    {
        public void Apply(AnotherEvent e)
        {

        }
    }

    public class AnotherEvent : DomainEvent<AnotherAggregate>
    {

    }
}
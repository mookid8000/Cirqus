using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using d60.Cirqus.Aggregates;
using d60.Cirqus.Commands;
using d60.Cirqus.Events;
using d60.Cirqus.Extensions;
using d60.Cirqus.Numbers;
using d60.Cirqus.Views.ViewManagers;
using d60.Cirqus.Views.ViewManagers.Old;
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
            _context = RegisterForDisposal(new TestContext());
        }

        [Test]
        public void CopiesCommandHeadersToEventsLikeTheRealCommandProcessor()
        {
            // arrange
            var id1 = Guid.NewGuid();

            // act
            _context.ProcessCommand(new RootCommand(id1) {Meta = {{"custom-header", "hej!!!11"}}});

            // assert
            Assert.That(_context.History.OfType<RootEvent>().Single().Meta.ContainsKey("custom-header"), Is.True);
            Assert.That(_context.History.OfType<RootEvent>().Single().Meta["custom-header"], Is.EqualTo("hej!!!11"));
        }


        [Test]
        public void CanGetFullyHydratedAggregateRootOutsideOfUnitOfWork()
        {
            // arrange
            var id1 = Guid.NewGuid();
            var id2 = Guid.NewGuid();
            _context.ProcessCommand(new RootCommand(id1));
            _context.ProcessCommand(new RootCommand(id1));
            _context.ProcessCommand(new RootCommand(id2));

            // act
            var root1 = _context.AggregateRootsInHistory.OfType<Root>().Single(i => i.Id == id1);
            var root2 = _context.AggregateRootsInHistory.OfType<Root>().Single(i => i.Id == id2);

            // assert
            Assert.That(root1.DidStuffCount, Is.EqualTo(2));
            Assert.That(root2.DidStuffCount, Is.EqualTo(1));
        }

        public class Root : AggregateRoot, IEmit<RootEvent>
        {
            public int DidStuffCount { get; set; }

            public void DoStuff()
            {
                Emit(new RootEvent());
            }

            public void Apply(RootEvent e)
            {
                DidStuffCount++;
            }
        }

        public class RootCommand : Command<Root>
        {
            public RootCommand(Guid aggregateRootId)
                : base(aggregateRootId)
            {
            }

            public override void Execute(Root aggregateRoot)
            {
                aggregateRoot.DoStuff();
            }
        }

        public class RootEvent : DomainEvent<Root>
        {
        }


        [Test]
        public void CanWriteEventHistory()
        {
            var aggregateRoot1Id = new Guid("03af8b3e-1f9f-4143-90ad-c22bb978210f");
            var aggregateRoot2Id = new Guid("82d07316-4891-4806-96cf-c42d2e011df3");

            _context.Save(aggregateRoot1Id, new EventForThatRoot());

            _context.Save(aggregateRoot2Id, new EventForThatRoot());
            _context.Save(aggregateRoot2Id, new EventForThatRoot());

            var builder = new StringBuilder();
            _context.History.WriteTo(new StringWriter(builder));

            var linesWithRootIds = builder.ToString()
                .Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries)
                .Where(line => line.Contains(DomainEvent.MetadataKeys.AggregateRootId))
                .Select(line => line.Trim().Replace(" ", "").Replace(",", ""))
                .ToArray();

            _context.History.WriteTo(Console.Out);

            Console.WriteLine(string.Join(Environment.NewLine, linesWithRootIds));

            Assert.That(linesWithRootIds, Is.EqualTo(new[]
            {
                @"""root_id"":""03af8b3e-1f9f-4143-90ad-c22bb978210f""",
                @"""root_id"":""82d07316-4891-4806-96cf-c42d2e011df3""",
                @"""root_id"":""82d07316-4891-4806-96cf-c42d2e011df3""",
            }));
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
        public void CanDispatchToViewsImmediatelyOnSave()
        {
            var viewMan = new SillyViewManager();
            _context.AddViewManager(viewMan);
            var aggregateRootId = Guid.NewGuid();

            _context.Save(aggregateRootId, new AnEvent());

            Assert.That(viewMan.ReceivedDomainEvents.Count, Is.EqualTo(1));
        }

        [Test]
        public void VerifiesThatEventsCanBeSerialized()
        {
            Assert.Throws<ArgumentException>(() => _context.Save(Guid.NewGuid(), UnserializableDomainEvent.Create("hej der!")));
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
            using (var uow = _context.BeginUnitOfWork())
            {
                var root = uow.Get<AnAggregate>(rootId);

                // act
                root.DoStuff();

                // assert
                Assert.That(uow.EmittedEvents.Cast<AnEvent>().Single(), Is.TypeOf<AnEvent>());
                Assert.That(uow.EmittedEvents.Cast<AnEvent>().Single().GetAggregateRootId(), Is.EqualTo(rootId));
            }
        }

        [Test]
        public void CommittedEventsBecomeTheHistory()
        {
            // arrange
            var rootId = Guid.NewGuid();
            using (var uow = _context.BeginUnitOfWork())
            {
                var root = uow.Get<AnAggregate>(rootId);
                root.DoStuff();

                // act
                uow.Commit();

                // assert
                Assert.That(_context.History.Cast<AnEvent>().Single(), Is.TypeOf<AnEvent>());
                Assert.That(_context.History.Cast<AnEvent>().Single().GetAggregateRootId(), Is.EqualTo(rootId));
            }
        }

        [Test]
        public void EventsAreNotAppendedToHistoryBeforeCommit()
        {
            // arrange
            var rootId = Guid.NewGuid();
            using (var uow = _context.BeginUnitOfWork())
            {
                var root = uow.Get<AnAggregate>(rootId);
                root.DoStuff();

                Assert.That(_context.History.Count(), Is.EqualTo(0));
            }
        }

        [Test]
        public void CommittedEventsAreStillAvailableInUnitOfWorkAfterCommit()
        {
            // arrange
            var rootId = Guid.NewGuid();
            using (var uow = _context.BeginUnitOfWork())
            {
                var root = uow.Get<AnAggregate>(rootId);
                root.DoStuff();

                // act
                uow.Commit();

                // assert
                Assert.That(uow.EmittedEvents.Count(), Is.EqualTo(1));
            }
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
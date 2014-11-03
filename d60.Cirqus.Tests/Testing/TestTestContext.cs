using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using d60.Cirqus.Aggregates;
using d60.Cirqus.Commands;
using d60.Cirqus.Events;
using d60.Cirqus.Extensions;
using d60.Cirqus.Numbers;
using d60.Cirqus.Views;
using d60.Cirqus.Views.ViewManagers;
using NUnit.Framework;
using TestContext = d60.Cirqus.Testing.TestContext;

namespace d60.Cirqus.Tests.Testing
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
        public void CanGetSpecialMetaFields()
        {
            var myBirthday = new DateTime(1979, 3, 19, 12, 30, 00, DateTimeKind.Utc);
            _context.SetCurrentTime(myBirthday);

            _context.Save("root_id", new RandomEvent());

            var domainEvent = _context.History.Single();
            Assert.That(domainEvent.GetUtcTime(), Is.EqualTo(myBirthday));
            Assert.That(domainEvent.GetAggregateRootId(), Is.EqualTo("root_id"));

        }

        class RandomEvent : DomainEvent<Root>
        {
        }


        [Test]
        public void CopiesCommandHeadersToEventsLikeTheRealCommandProcessor()
        {
            _context.ProcessCommand(new RootCommand("id1") {Meta = {{"custom-header", "hej!!!11"}}});

            Assert.That(_context.History.OfType<RootEvent>().Single().Meta.ContainsKey("custom-header"), Is.True);
            Assert.That(_context.History.OfType<RootEvent>().Single().Meta["custom-header"], Is.EqualTo("hej!!!11"));
        }


        [Test]
        public void CanGetFullyHydratedAggregateRootOutsideOfUnitOfWork()
        {
            // arrange
            var id1 = "id1";
            var id2 = "id2";
            _context.ProcessCommand(new RootCommand(id1));
            _context.ProcessCommand(new RootCommand(id1));
            _context.ProcessCommand(new RootCommand(id2));

            // act
            var root1 = _context.AggregateRoots.OfType<Root>().Single(i => i.Id == id1);
            var root2 = _context.AggregateRoots.OfType<Root>().Single(i => i.Id == id2);

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
            public RootCommand(string aggregateRootId)
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
            _context.Save("rootid1", new EventForThatRoot());
            _context.Save("rootid2", new EventForThatRoot());
            _context.Save("rootid2", new EventForThatRoot());

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
                @"""root_id"":""rootid1",
                @"""root_id"":""rootid2",
                @"""root_id"":""rootid2",
            }));
        }


        [Test]
        public void AlsoPicksUpMetadataFromAggregate()
        {
            // arrange
            // act
            _context.Save("rootid", new EventForThatRoot());

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

            _context.Save("rootid", new AnEvent());

            _context.WaitForViewsToCatchUp();

            Assert.That(viewMan.ReceivedDomainEvents.Count, Is.EqualTo(1));
        }

        [Test]
        public void VerifiesThatEventsCanBeSerialized()
        {
            Assert.Throws<ArgumentException>(() => _context.Save("rootid", UnserializableDomainEvent.Create("hej der!")));
        }

        class UnserializableDomainEvent : DomainEvent<AnAggregate>
        {
            public static UnserializableDomainEvent Create(string text)
            {
                return new UnserializableDomainEvent { MyString = text };
            }

            public string MyString { get; protected set; }
        }


        class SillyViewManager : IViewManager
        {
            long _position = -1;

            public SillyViewManager()
            {
                ReceivedDomainEvents = new List<DomainEvent>();
            }
            
            public List<DomainEvent> ReceivedDomainEvents { get; set; }
            
            public long GetPosition(bool canGetFromCache = true)
            {
                return _position;
            }

            public void Dispatch(IViewContext viewContext, IEnumerable<DomainEvent> batch)
            {
                ReceivedDomainEvents.AddRange(batch);

                if (!batch.Any()) return;

                _position = batch.Max(e => e.GetGlobalSequenceNumber());
            }

            public async Task WaitUntilProcessed(CommandProcessingResult result, TimeSpan timeout)
            {
                if (!result.EventsWereEmitted) return;

                while (_position < result.GetNewPosition())
                {
                    await Task.Delay(100);
                }
            }

            public void Purge()
            {
                ReceivedDomainEvents.Clear();
            }
        }


        [Test]
        public void HydratesEntitiesWithExistingEvents()
        {
            _context.Save("rootid", new AnEvent());
            _context.Save("rootid", new AnEvent());
            _context.Save("rootid", new AnEvent());

            var firstInstance = _context.BeginUnitOfWork().Get<AnAggregate>("rootid");

            Assert.That(firstInstance.ProcessedEvents, Is.EqualTo(3));
        }

        [Test]
        public void EmittedEventsAreCollectedInUnitOfWork()
        {
            // arrange
            using (var uow = _context.BeginUnitOfWork())
            {
                var root = uow.Get<AnAggregate>("rootid");

                // act
                root.DoStuff();

                // assert
                Assert.That(uow.EmittedEvents.Cast<AnEvent>().Single(), Is.TypeOf<AnEvent>());
                Assert.That(uow.EmittedEvents.Cast<AnEvent>().Single().GetAggregateRootId(), Is.EqualTo("rootid"));
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
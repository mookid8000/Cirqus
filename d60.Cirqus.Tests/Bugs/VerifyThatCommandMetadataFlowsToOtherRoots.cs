using System;
using System.Collections.Generic;
using System.Linq;
using d60.Cirqus.Aggregates;
using d60.Cirqus.Commands;
using d60.Cirqus.Events;
using d60.Cirqus.Serialization;
using d60.Cirqus.Testing.Internals;
using NUnit.Framework;
using TestContext = d60.Cirqus.Testing.TestContext;

namespace d60.Cirqus.Tests.Bugs
{
    [TestFixture]
    public class VerifyThatCommandMetadataFlowsToOtherRoots : FixtureBase
    {
        TestContext _context;
        ICommandProcessor _commandProcessor;
        InMemoryEventStore _inMemoryEventStore;

        protected override void DoSetUp()
        {
            _commandProcessor = CommandProcessor.With()
                .EventStore(e => e.Register(c =>
                {
                    _inMemoryEventStore = new InMemoryEventStore(c.Get<IDomainEventSerializer>());
                    return _inMemoryEventStore;
                }))
                .Create();

            _context = TestContext.Create();
        }

        [Test]
        public void FlowsCorrectlyTrivialCase()
        {
            ProcessCommand(new OrdinaryCommand("bimse")
            {
                Meta = {{"testkey", "testvalue"}}
            });

            VerifyEventMetadata(1);
        }

        public class OrdinaryCommand : Command<Root>
        {
            public OrdinaryCommand(string aggregateRootId)
                : base(aggregateRootId)
            {
            }

            public override void Execute(Root aggregateRoot)
            {
                aggregateRoot.DoStuff();
            }
        }

        [Test]
        public void FlowsCorrectlyViaCommandContext()
        {
            ProcessCommand(new GenericCommand
            {
                Meta = {{"testkey", "testvalue"}}
            });

            VerifyEventMetadata(2);
        }

        public class GenericCommand : ExecutableCommand
        {
            public override void Execute(ICommandContext context)
            {
                context.Create<Root>("root1").DoStuff();
                context.Create<Root>("root2").DoStuff();
            }
        }

        [Test]
        public void FlowsCorrectlyViaRootLoadingOtherRoot()
        {
            ProcessCommand(new TriggerRootLoadingOtherRoot
            {
                Meta = {{"testkey", "testvalue"}}
            });

            VerifyEventMetadata(1);
        }

        public class TriggerRootLoadingOtherRoot : ExecutableCommand
        {
            public override void Execute(ICommandContext context)
            {
                context
                    .Create<Root>("root1")
                    .MakeOtherRootDoStuff();
            }
        }

        public class Root : AggregateRoot, IEmit<Event>
        {
            public void DoStuff()
            {
                Emit(new Event());
            }

            public void Apply(Event e)
            {
            }

            public void MakeOtherRootDoStuff()
            {
                Create<Root>("root2").DoStuff();
            }
        }

        public class Event : DomainEvent<Root> { }

        void ProcessCommand(Command command)
        {
            _context.ProcessCommand(command);
            _commandProcessor.ProcessCommand(command);
        }

        void VerifyEventMetadata(int expectedNumberOfEvents)
        {
            VerifyEvents(expectedNumberOfEvents, _context.History.OfType<Event>(), "TestContext");

            VerifyEvents(expectedNumberOfEvents, _inMemoryEventStore.OfType<Event>(), "real CommandProcessor");
        }

        static void VerifyEvents(int expectedNumberOfEvents, IEnumerable<Event> events, string whichCommandProcessor)
        {
            if (events.Count() != expectedNumberOfEvents)
            {
                Assert.Fail(@"Number of events with {0} was not {1} as expected - got the following events:

{2}", whichCommandProcessor, expectedNumberOfEvents, string.Join(Environment.NewLine, events));
            }

            var eventsWithoutProperMetadata = events
                .Where(e => !e.Meta.ContainsKey("testkey") || e.Meta["testkey"] != "testvalue")
                .ToList();

            if (eventsWithoutProperMetadata.Any())
            {
                Assert.Fail(@"Found the following events (with {0}) without proper metadata:

{1}", whichCommandProcessor, string.Join(Environment.NewLine, eventsWithoutProperMetadata));
            }
        }
    }
}
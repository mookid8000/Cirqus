using System;
using System.Linq;
using d60.Cirqus.Aggregates;
using d60.Cirqus.Commands;
using d60.Cirqus.Events;
using NUnit.Framework;
using TestContext = d60.Cirqus.Testing.TestContext;

namespace d60.Cirqus.Tests.Bugs
{
    [TestFixture]
    public class VerifyThatCommandMetadataFlowsToOtherRoots : FixtureBase
    {
        TestContext _context;

        protected override void DoSetUp()
        {
            _context = TestContext.Create();
        }

        [Test]
        public void FlowsCorrectlyTrivialCase()
        {
            _context.ProcessCommand(new OrdinaryCommand("bimse")
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
            _context.ProcessCommand(new GenericCommand
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
            _context.ProcessCommand(new TriggerRootLoadingOtherRoot
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

        void VerifyEventMetadata(int expectedNumberOfEvents)
        {
            var events = _context.History
                .OfType<Event>()
                .ToList();

            if (events.Count != expectedNumberOfEvents)
            {
                Assert.Fail(@"Number of events was not {0} as expected - got the following events:

{1}", expectedNumberOfEvents, string.Join(Environment.NewLine, events));
            }

            var eventsWithoutProperMetadata = events
                .Where(e => !e.Meta.ContainsKey("testkey") || e.Meta["testkey"] != "testvalue")
                .ToList();

            if (eventsWithoutProperMetadata.Any())
            {
                Assert.Fail(@"Found the following events without proper metadata:

{0}", string.Join(Environment.NewLine, eventsWithoutProperMetadata));
            }
        }
    }
}
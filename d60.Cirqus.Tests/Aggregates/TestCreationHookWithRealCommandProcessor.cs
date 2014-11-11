using System.Linq;
using d60.Cirqus.Aggregates;
using d60.Cirqus.Commands;
using d60.Cirqus.Events;
using d60.Cirqus.Serialization;
using d60.Cirqus.Testing.Internals;
using d60.Cirqus.Tests.Stubs;
using NUnit.Framework;

namespace d60.Cirqus.Tests.Aggregates
{
    [TestFixture]
    public class TestCreationHookWithRealCommandProcessor : FixtureBase
    {
        readonly JsonDomainEventSerializer _domainEventSerializer = new JsonDomainEventSerializer();
        ICommandProcessor _commandProcessor;
        InMemoryEventStore _eventStore;

        protected override void DoSetUp()
        {
            _eventStore = new InMemoryEventStore(_domainEventSerializer);
            var aggregateRootRepository = new DefaultAggregateRootRepository(_eventStore, _domainEventSerializer);
            _commandProcessor = new CommandProcessor(_eventStore, aggregateRootRepository, new ConsoleOutEventDispatcher(), _domainEventSerializer, new DefaultCommandMapper());
            RegisterForDisposal(_commandProcessor);
        }

        [Test]
        public void InvokesCreatedHookWhenAggregateRootIsFirstCreated()
        {
            _commandProcessor.ProcessCommand(new MakeRootDoSomething("id1"));

            var expectedSequenceOfEvents = new[] { typeof(RootCreated), typeof(RootDidSomething) };
            var actualSequenceOfEvents = _eventStore.Select(e => e.GetType()).ToArray();

            Assert.That(actualSequenceOfEvents, Is.EqualTo(expectedSequenceOfEvents));
        }

        [Test]
        public void InvokesCreatedHookWhenAggregateRootIsFirstCreatedAndNeverAgain()
        {
            // arrange

            // act
            _commandProcessor.ProcessCommand(new MakeRootDoSomething("rootid"));
            _commandProcessor.ProcessCommand(new MakeRootDoSomething("rootid"));
            _commandProcessor.ProcessCommand(new MakeRootDoSomething("rootid"));

            // assert
            var expectedSequenceOfEvents = new[]
            {
                typeof(RootCreated), 
                typeof(RootDidSomething), 
                typeof(RootDidSomething), 
                typeof(RootDidSomething)
            };
            var actualSequenceOfEvents = _eventStore.Select(e => e.GetType()).ToArray();

            Assert.That(actualSequenceOfEvents, Is.EqualTo(expectedSequenceOfEvents));
        }

        public class MakeRootDoSomething : Command<Root>
        {
            public MakeRootDoSomething(string aggregateRootId) 
                : base(aggregateRootId)
            {
            }

            public override void Execute(Root aggregateRoot)
            {
                aggregateRoot.DoSomething();
            }
        }

        public class Root : AggregateRoot,
            IEmit<RootCreated>,
            IEmit<RootDidSomething>
        {
            protected override void Created()
            {
                Emit(new RootCreated());
            }

            public void DoSomething()
            {
                Emit(new RootDidSomething());
            }

            public void Apply(RootCreated e)
            {

            }

            public void Apply(RootDidSomething e)
            {

            }
        }

        public class RootCreated : DomainEvent<Root> { }
        public class RootDidSomething : DomainEvent<Root> { }
    }
}
using System;
using System.Linq;
using d60.Cirqus.Aggregates;
using d60.Cirqus.Commands;
using d60.Cirqus.Events;
using d60.Cirqus.MongoDb.Events;
using d60.Cirqus.Serialization;
using d60.Cirqus.Tests.MongoDb;
using d60.Cirqus.Tests.Stubs;
using d60.Cirqus.Views;
using NUnit.Framework;
using TestContext = d60.Cirqus.Testing.TestContext;

namespace d60.Cirqus.Tests.Bugs
{
    [TestFixture, Description("Verify bug in being able to correctly load active aggregate roots from the current unit of work")]
    public class VerifyThatDuplicateSequenceNumbersAreNotAssigned : FixtureBase
    {
        readonly JsonDomainEventSerializer _domainEventSerializer = new JsonDomainEventSerializer();

        [Test, Category(TestCategories.MongoDb)]
        public void NoProblemoWithRealSetup()
        {
            // arrange
            var eventStore = new MongoDbEventStore(MongoHelper.InitializeTestDatabase(), "events");

            var commandProcessor = CommandProcessor.With()
                .EventStore(e => e.Registrar.Register<IEventStore>(c => eventStore))
                .EventDispatcher(e => e.Registrar.Register<IEventDispatcher>(c => new ConsoleOutEventDispatcher()))
                .Create();

            RegisterForDisposal(commandProcessor);

            // make sure all roots exist
            Console.WriteLine("Processing initial two commands");
            commandProcessor.ProcessCommand(new DoSomethingToABunchOfRootsCommand("id1"));
            commandProcessor.ProcessCommand(new DoSomethingToABunchOfRootsCommand("id2"));

            // act
            Console.WriteLine("\r\n\r\nActing...");
            commandProcessor.ProcessCommand(new DoSomethingToABunchOfRootsCommand("id1", "id2"));

            // assert
        }

        [Test]
        public void NoProblemoWithTestContext()
        {
            // arrange
            var context = RegisterForDisposal(new TestContext());

            try
            {
                // make sure all roots exist
                Console.WriteLine("Processing initial two commands");
                context.ProcessCommand(new DoSomethingToABunchOfRootsCommand("id1"));
                context.ProcessCommand(new DoSomethingToABunchOfRootsCommand("id2"));

                // act
                Console.WriteLine("\r\n\r\nActing...");
                context.ProcessCommand(new DoSomethingToABunchOfRootsCommand("id1", "id2"));
            }
            finally
            {
                context.History.WriteTo(Console.Out);
            }
            // assert
        }

        public class DoSomethingToABunchOfRootsCommand : Command<SomeRoot>
        {
            public string[] AdditionalRootIds { get; set; }

            public DoSomethingToABunchOfRootsCommand(string aggregateRootId, params string[] additionalRootIds) 
                : base(aggregateRootId)
            {
                AdditionalRootIds = additionalRootIds;
            }

            public override void Execute(SomeRoot aggregateRoot)
            {
                aggregateRoot.DoSomething(AdditionalRootIds);

                aggregateRoot.DoSomethingElse();
            }
        }

        public class SomeRoot : AggregateRoot, IEmit<SomethingHappened>, IEmit<SomethingElseHappened>
        {
            public void DoSomething(params string[] idsToDoSomethingTo)
            {
                Emit(new SomethingHappened());

                idsToDoSomethingTo.ToList().ForEach(id => Load<SomeRoot>(id).DoSomething());
                
                idsToDoSomethingTo.ToList().ForEach(id => Load<SomeRoot>(id).DoSomethingElse());
            }

            public void DoSomethingElse()
            {
                Emit(new SomethingElseHappened());
            }

            public void Apply(SomethingHappened e)
            {
            }

            public void Apply(SomethingElseHappened e)
            {
            }
        }

        public class SomethingHappened : DomainEvent<SomeRoot>
        {
        }

        public class SomethingElseHappened : DomainEvent<SomeRoot>
        {
        }
    }

}
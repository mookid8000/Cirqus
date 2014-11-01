using System;
using System.Linq;
using d60.Cirqus.Aggregates;
using d60.Cirqus.Commands;
using d60.Cirqus.Events;
using d60.Cirqus.MongoDb.Events;
using d60.Cirqus.Serialization;
using d60.Cirqus.Tests.MongoDb;
using d60.Cirqus.Tests.Stubs;
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
            var commandProcessor = new CommandProcessor(eventStore, new DefaultAggregateRootRepository(eventStore, _domainEventSerializer), new ConsoleOutEventDispatcher(),
                _domainEventSerializer);

            RegisterForDisposal(commandProcessor);

            var root1Id = new Guid("10000000-0000-0000-0000-000000000000");
            var root2Id = new Guid("20000000-0000-0000-0000-000000000000");

            // make sure all roots exist
            Console.WriteLine("Processing initial two commands");
            commandProcessor.ProcessCommand(new DoSomethingToABunchOfRootsCommand(root1Id));
            commandProcessor.ProcessCommand(new DoSomethingToABunchOfRootsCommand(root2Id));

            // act
            Console.WriteLine("\r\n\r\nActing...");
            commandProcessor.ProcessCommand(new DoSomethingToABunchOfRootsCommand(root1Id, root2Id));

            // assert
        }

        [Test]
        public void NoProblemoWithTestContext()
        {
            // arrange
            var context = RegisterForDisposal(new TestContext());

            try
            {
                var root1Id = Guid.NewGuid();
                var root2Id = Guid.NewGuid();

                // make sure all roots exist
                Console.WriteLine("Processing initial two commands");
                context.ProcessCommand(new DoSomethingToABunchOfRootsCommand(root1Id));
                context.ProcessCommand(new DoSomethingToABunchOfRootsCommand(root2Id));

                // act
                Console.WriteLine("\r\n\r\nActing...");
                context.ProcessCommand(new DoSomethingToABunchOfRootsCommand(root1Id, root2Id));
            }
            finally
            {
                context.History.WriteTo(Console.Out);
            }
            // assert
        }

        public class DoSomethingToABunchOfRootsCommand : Command<SomeRoot>
        {
            public Guid[] AdditionalRootIds { get; set; }

            public DoSomethingToABunchOfRootsCommand(Guid aggregateRootId, params Guid[] additionalRootIds) : base(aggregateRootId)
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
            public void DoSomething(params Guid[] idsToDoSomethingTo)
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
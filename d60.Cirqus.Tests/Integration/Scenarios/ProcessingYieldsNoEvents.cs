using System;
using d60.Cirqus.Aggregates;
using d60.Cirqus.Commands;
using NUnit.Framework;

namespace d60.Cirqus.Tests.Integration.Scenarios
{
    [TestFixture]
    public class ProcessingYieldsNoEvents : IntegrationTestBase
    {
        [TestCase(EventStoreOption.InMemory)]
        [TestCase(EventStoreOption.SqlServer)]
        [TestCase(EventStoreOption.MongoDb)]
        [TestCase(EventStoreOption.Postgres)]
        public void Run(EventStoreOption eventStoreOption)
        {
            var commandProcessor = GetCommandProcessor(eventStoreOption);

            commandProcessor.ProcessCommand(new SomeCommand { SomeId = "somekey" });
        }


        public class SomeRoot : AggregateRoot
        {
            public void PossiblyDoSomething()
            {
                // no way!
            }
        }

        public class SomeCommand : Command
        {
            public string SomeId { get; set; }

            public override void Execute(ICommandContext context)
            {
                var root = context.Load<SomeRoot>(SomeId, createIfNotExists: true);

                root.PossiblyDoSomething();
            }
        }
    }
}
using System;
using System.Configuration;
using d60.Cirqus.Commands;
using d60.Cirqus.Config;
using d60.Cirqus.MongoDb.Config;
using NUnit.Framework;

namespace d60.Cirqus.Tests.Config
{
    [TestFixture]
    public class TestConfigurationApi : FixtureBase
    {
        [Test, Category(TestCategories.MongoDb)]
        public void CanDoTheConfigThing()
        {
            var mongoConnectionString = ConfigurationManager.ConnectionStrings["mongotestdb"];

            var processor = CommandProcessor.With()
                .Logging(l => l.UseConsole())
                .EventStore(e => e.UseMongoDb(mongoConnectionString.ConnectionString, "Events"))
                .AggregateRootRepository(r =>
                {
                    r.UseDefault();
                    r.EnableInMemorySnapshotCaching(10000);
                })
                .EventDispatcher(d => d.UseViewManagerEventDispatcher())
                .Options(o =>
                {
                    o.PurgeExistingViews(true);
                    o.AddDomainExceptionType<ApplicationException>();
                })
                .Create();

            var someCommand = new SomeCommand();
            processor.ProcessCommand(someCommand);

            Assert.That(someCommand.WasProcessed, Is.EqualTo(true));
        }

        public class SomeCommand : Command
        {
            public bool WasProcessed { get; set; }
            
            public override void Execute(ICommandContext context)
            {
                WasProcessed = true;
            }
        }
    }
}
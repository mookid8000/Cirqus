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
        [Test]
        public void CanDoTheConfigThing()
        {
            var mongoConnectionString = ConfigurationManager.ConnectionStrings["mongotestdb"];

            var processor = CommandProcessor.With()
                .EventStore(e => e.StoreInMongoDb(mongoConnectionString.ConnectionString, "Events"))
                .AggregateRootRepository(r => r.UseDefaultAggregateRootRepository())
                .EventDispatcher(d => d.ViewManagerEventDispatcher())
                .Options(o => o.PurgeViewsAtStartup(true))
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
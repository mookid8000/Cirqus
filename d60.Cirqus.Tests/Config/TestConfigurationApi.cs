using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading;
using d60.Cirqus.Aggregates;
using d60.Cirqus.Commands;
using d60.Cirqus.Config;
using d60.Cirqus.Config.Configurers;
using d60.Cirqus.Events;
using d60.Cirqus.Extensions;
using d60.Cirqus.MongoDb.Config;
using d60.Cirqus.MongoDb.Views.New;
using d60.Cirqus.Tests.MongoDb;
using d60.Cirqus.Views.ViewManagers;
using d60.Cirqus.Views.ViewManagers.Locators;
using NUnit.Framework;

namespace d60.Cirqus.Tests.Config
{
    [TestFixture]
    public class TestConfigurationApi : FixtureBase
    {
        [Test]
        public void CanInstallMultipleEventDispatchers()
        {
            var database = MongoHelper.InitializeTestDatabase();
            var mongoConnectionString = ConfigurationManager.ConnectionStrings["mongotestdb"];

            var commandProcessor = CommandProcessor.With()
                .EventStore(e => e.UseMongoDb(mongoConnectionString.ConnectionString, "Events"))
                .EventDispatcher(d =>
                {
                    d.UseNewViewManagerEventDispatcher(new NewMongoDbViewManager<ConfigTestView>(database, "view1"));
                    d.UseNewViewManagerEventDispatcher(new NewMongoDbViewManager<ConfigTestView>(database, "view2"));
                    d.UseNewViewManagerEventDispatcher(new NewMongoDbViewManager<ConfigTestView>(database, "view3"));
                    d.UseNewViewManagerEventDispatcher(new NewMongoDbViewManager<ConfigTestView>(database, "view4"));
                })
                .Create();

            var id1 = Guid.NewGuid();
            var id2 = Guid.NewGuid();

            commandProcessor.ProcessCommand(new ConfigTestCommand(id1));
            commandProcessor.ProcessCommand(new ConfigTestCommand(id1));
            commandProcessor.ProcessCommand(new ConfigTestCommand(id1));
            commandProcessor.ProcessCommand(new ConfigTestCommand(id2));
            commandProcessor.ProcessCommand(new ConfigTestCommand(id2));

            Thread.Sleep(1000);

            Assert.That(database.GetCollectionNames().OrderBy(n => n).Where(c => c.StartsWith("view")).ToArray(),
                Is.EqualTo(new[] {"view1", "view2", "view3", "view4"}));
        }

        public class ConfigTestView : IViewInstance<GlobalInstanceLocator>, ISubscribeTo<ConfigTestEvent>
        {
            public ConfigTestView()
            {
                CountsByRootId = new Dictionary<Guid, int>();
            }
            public string Id { get; set; }
            public long LastGlobalSequenceNumber { get; set; }
            public Dictionary<Guid, int> CountsByRootId { get; set; }
            public void Handle(IViewContext context, ConfigTestEvent domainEvent)
            {
                var id = domainEvent.GetAggregateRootId();

                if (!CountsByRootId.ContainsKey(id))
                    CountsByRootId[id] = 0;

                CountsByRootId[id]++;
            }
        }

        public class ConfigTestRoot : AggregateRoot, IEmit<ConfigTestEvent>
        {
            public int EmittedEvents { get; set; }

            public void EmitStuff()
            {
                Emit(new ConfigTestEvent());
            }

            public void Apply(ConfigTestEvent e)
            {
                EmittedEvents++;
            }
        }

        public class ConfigTestEvent : DomainEvent<ConfigTestRoot> { }

        public class ConfigTestCommand : Command<ConfigTestRoot>
        {
            public ConfigTestCommand(Guid aggregateRootId) : base(aggregateRootId)
            {
            }

            public override void Execute(ConfigTestRoot aggregateRoot)
            {
                aggregateRoot.EmitStuff();
            }
        }

        [Test, Category(TestCategories.MongoDb)]
        public void CanDoTheConfigThing()
        {
            var mongoConnectionString = ConfigurationManager.ConnectionStrings["mongotestdb"];

            var fullConfiguration = CommandProcessor.With()
                .Logging(l => l.UseConsole())
                .EventStore(e => e.UseMongoDb(mongoConnectionString.ConnectionString, "Events"))
                .AggregateRootRepository(r => r.EnableInMemorySnapshotCaching(10000))
                .EventDispatcher(d =>
                {
                    d.UseViewManagerEventDispatcher();
                    d.UseNewViewManagerEventDispatcher();
                })
                .Options(o =>
                {
                    o.PurgeExistingViews(true);
                    o.AddDomainExceptionType<ApplicationException>();
                    o.SetMaxRetries(10);
                });

            ((CommandProcessorConfigurationBuilder)fullConfiguration).LogServicesTo(Console.Out);

            var processor = fullConfiguration.Create();

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
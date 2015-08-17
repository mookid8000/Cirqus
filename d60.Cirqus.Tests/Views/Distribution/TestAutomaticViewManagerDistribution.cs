using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using d60.Cirqus.Extensions;
using d60.Cirqus.Logging;
using d60.Cirqus.Logging.Console;
using d60.Cirqus.MongoDb.Config;
using d60.Cirqus.MongoDb.Views;
using d60.Cirqus.Tests.MongoDb;
using d60.Cirqus.Tests.Views.Distribution.Model;
using d60.Cirqus.Views;
using MongoDB.Driver;
using NUnit.Framework;

namespace d60.Cirqus.Tests.Views.Distribution
{
    [TestFixture, Description("let's see if we can make view managers be distributed evently among several machines when running e.g. as an auto-scaled Azure web site")]
    public class TestAutomaticViewManagerDistribution : FixtureBase
    {
        MongoDatabase _mongoDatabase;

        protected override void DoSetUp()
        {
            CirqusLoggerFactory.Current = new ConsoleLoggerFactory(minLevel:Logger.Level.Warn);

            _mongoDatabase = MongoHelper.InitializeTestDatabase();
        }

        [Test, Ignore("For some reason, this test fails from time to time. The auto-distribution feature is not quite ready for primetime yet.")]
        public void CanAccelerateViewsByPartitioning()
        {
            // SomeRootView is intentionally slow - it takes ~ 1 second to process each event, so unless we process these
            // two bad boys in parallel, we're not going to make it in 15 seconds
            var viewManagers = new[]
            {
                new MongoDbViewManager<SomeRootView>(_mongoDatabase, "view1", "Position"), 
                new MongoDbViewManager<SomeRootView>(_mongoDatabase, "view2", "Position"),
                new MongoDbViewManager<SomeRootView>(_mongoDatabase, "view3", "Position"),
                new MongoDbViewManager<SomeRootView>(_mongoDatabase, "view4", "Position"),
                new MongoDbViewManager<SomeRootView>(_mongoDatabase, "view5", "Position"), 
                new MongoDbViewManager<SomeRootView>(_mongoDatabase, "view6", "Position"),
                new MongoDbViewManager<SomeRootView>(_mongoDatabase, "view7", "Position"),
                new MongoDbViewManager<SomeRootView>(_mongoDatabase, "view8", "Position"),
            };

            var firstCommandProcessor = CreateCommandProcessor("1", viewManagers);
           
            CreateCommandProcessor("2", viewManagers);
            
            CreateCommandProcessor("3", viewManagers);
            
            CreateCommandProcessor("4", viewManagers);
            
            CreateCommandProcessor("5", viewManagers);
            
            CreateCommandProcessor("6", viewManagers);
            
            CreateCommandProcessor("7", viewManagers);
            
            CreateCommandProcessor("8", viewManagers);

            var lastResult = Enumerable.Range(0, 20)
                .Select(i => firstCommandProcessor.ProcessCommand(new MakeSomeRootDoStuff("bimse")))
                .Last();

            var goal = TimeSpan.FromSeconds(45);

            using (var statusTimer = new Timer(5000))
            {
                var state = new MongoDbAutoDistributionState(_mongoDatabase, "AutoDistribution");
                statusTimer.Elapsed += (o, ea) =>
                {
                    Console.WriteLine(@"--------------------------------------------
Distribution:
{0}

Views:
{1}",
    string.Join(Environment.NewLine, state.GetCurrentState().Select(s => string.Format("    {0}: {1}", s.ManagerId, string.Join(", ", s.ViewIds)))),
    string.Join(Environment.NewLine, viewManagers.Select(v => string.Format("    {0}: {1}", v.GetPosition().Result, v.GetType().GetPrettyName()))));
                };
                statusTimer.Start();

                Task.WaitAll(viewManagers.Select(v => v.WaitUntilProcessed(lastResult, goal)).ToArray());
            }
        }

        ICommandProcessor CreateCommandProcessor(string id, IEnumerable<IViewManager> viewManagers)
        {
            var commandProcessor = CommandProcessor.With()
                .EventStore(e => e.UseMongoDb(_mongoDatabase, "Events"))
                .EventDispatcher(e =>
                {
                    e.UseViewManagerEventDispatcher(viewManagers.ToArray())
                        .AutomaticallyRedistributeViews(id, new MongoDbAutoDistributionState(_mongoDatabase, "AutoDistribution"))
                        .WithMaxDomainEventsPerBatch(1);
                })
                .Create();

            RegisterForDisposal(commandProcessor);

            return commandProcessor;
        }
    }
}
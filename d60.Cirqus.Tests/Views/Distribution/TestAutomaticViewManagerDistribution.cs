using System;
using System.Collections.Generic;
using System.Linq;
using d60.Cirqus.Logging;
using d60.Cirqus.Logging.Console;
using d60.Cirqus.MongoDb.Config;
using d60.Cirqus.MongoDb.Views;
using d60.Cirqus.Tests.MongoDb;
using d60.Cirqus.Tests.Views.Distribution.Model;
using d60.Cirqus.Views;
using d60.Cirqus.Views.ViewManagers;
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
            CirqusLoggerFactory.Current = new ConsoleLoggerFactory();

            _mongoDatabase = MongoHelper.InitializeTestDatabase();
        }

        [Test]
        public void CanAccelerateViewsByPartitioning()
        {
            // SomeRootView is intentionally slow - it takes ~ 1 second to process each event, so unless we process these
            // two bad boys in parallel, we're not going to make it in 15 seconds
            var viewManagers = new[]
            {
                new MongoDbViewManager<SomeRootView>(_mongoDatabase, "view1"), 
                new MongoDbViewManager<SomeRootView>(_mongoDatabase, "view2")
            };

            var firstWaitHandle = new ViewManagerWaitHandle();
            var secondWaitHandle = new ViewManagerWaitHandle();

            var firstCommandProcessor = CreateCommandProcessor("1", firstWaitHandle, viewManagers);
           
            CreateCommandProcessor("2", secondWaitHandle, viewManagers);

            var lastResult = Enumerable.Range(0, 10)
                .Select(i => firstCommandProcessor.ProcessCommand(new MakeSomeRootDoStuff("bimse")))
                .Last();

            var goal = TimeSpan.FromSeconds(15);

            firstWaitHandle.WaitForAll(lastResult, goal).Wait();
        }

        ICommandProcessor CreateCommandProcessor(string id, ViewManagerWaitHandle waitHandle, IEnumerable<IViewManager> viewManagers)
        {
            var commandProcessor = CommandProcessor.With()
                .EventStore(e => e.UseMongoDb(_mongoDatabase, "Events"))
                .EventDispatcher(e =>
                {
                    e.UseViewManagerEventDispatcher(viewManagers.ToArray())
                        .WithWaitHandle(waitHandle)
                        .AutomaticallyRedistributeViews(id, new MongoDbAutoDistributionPersistence(_mongoDatabase, "AutoDistribution"));
                })
                .Create();

            RegisterForDisposal(commandProcessor);

            return commandProcessor;
        }
    }
}
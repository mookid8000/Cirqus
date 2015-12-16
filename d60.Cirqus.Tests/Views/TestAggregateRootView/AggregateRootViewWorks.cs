using System;
using System.Diagnostics;
using System.Threading.Tasks;
using d60.Cirqus.Extensions;
using d60.Cirqus.Logging;
using d60.Cirqus.Logging.Console;
using d60.Cirqus.MongoDb.Config;
using d60.Cirqus.Tests.Views.TestAggregateRootView.Model;
using d60.Cirqus.Views.ViewManagers;
using d60.Cirqus.Views.ViewManagers.Locators;
using NUnit.Framework;

namespace d60.Cirqus.Tests.Views.TestAggregateRootView
{
    [TestFixture]
    public class AggregateRootViewWorks : FixtureBase
    {
        ICommandProcessor _commandProcessor;
        InMemoryViewManager<AggregateRootView> _viewManager;
        ViewManagerWaitHandle _waitHandler;

        protected override void DoSetUp()
        {
            CirqusLoggerFactory.Current = new ConsoleLoggerFactory(minLevel: Logger.Level.Info);

            _viewManager = new InMemoryViewManager<AggregateRootView>();

            _waitHandler = new ViewManagerWaitHandle();

            _commandProcessor = CommandProcessor.With()
                .EventStore(e => e.UseMongoDb("mongodb://localhost/cirqus_bimse", "Events"))
                .EventDispatcher(e => e.UseViewManagerEventDispatcher(_viewManager).WithWaitHandle(_waitHandler))
                .Create();

            RegisterForDisposal(_commandProcessor);
        }

        [Test]
        public async Task YeahItWorks()
        {
            //1000.Times(() =>
            //{
            //    _commandProcessor.ProcessCommand(new IncrementNumberCommand("id1"));
            //});

            var stopwatch = Stopwatch.StartNew();

            await _waitHandler.WaitForAll(CommandProcessingResult.WithNewPosition(1000), TimeSpan.FromSeconds(100));

            Console.WriteLine("Catch-up took {0:0.0} s", stopwatch.Elapsed.TotalSeconds);
        }

        class AggregateRootView : IViewInstance<InstancePerAggregateRootLocator>, ISubscribeTo<NumberEvent>
        {
            static DateTime _lastOutput = DateTime.UtcNow;

            public string Id { get; set; }
            public long LastGlobalSequenceNumber { get; set; }

            public int Counter { get; set; }

            public int OtherCounter { get; set; }

            public void Handle(IViewContext context, NumberEvent domainEvent)
            {
                OtherCounter++;

                if (OtherCounter % 3 == 0)
                {

                    var aggregateRootId = domainEvent.GetAggregateRootId();
                    var instance = context.Load<AggregateRootWithLogic>(aggregateRootId);
                    Counter = instance.Counter;
                }

                if (OtherCounter % 100 == 0)
                {
                    var now = DateTime.UtcNow;

                    var elapsedSinceLastOutput = now - _lastOutput;

                    _lastOutput = now;

                    Console.WriteLine("Counter: {0} (elapsed: {1:0.0} s)", Counter, elapsedSinceLastOutput.TotalSeconds);
                }
            }
        }
    }
}
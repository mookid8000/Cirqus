using System;
using System.Linq;
using System.Threading;
using d60.Cirqus.Logging;
using d60.Cirqus.Logging.Console;
using d60.Cirqus.Testing;
using d60.Cirqus.Tests.Contracts.Views.Factories;
using d60.Cirqus.Tests.Contracts.Views.Models.RecoveryTest;
using d60.Cirqus.Views.ViewManagers;
using d60.Cirqus.Views.ViewManagers.Locators;
using NUnit.Framework;
using TestContext = d60.Cirqus.Testing.TestContext;
using Timer = System.Timers.Timer;

namespace d60.Cirqus.Tests.Contracts.Views
{
    [TestFixture(typeof(MongoDbViewManagerFactory), Category = TestCategories.MongoDb)]
    [TestFixture(typeof(PostgreSqlViewManagerFactory), Category = TestCategories.PostgreSql)]
    [TestFixture(typeof(MsSqlViewManagerFactory), Category = TestCategories.MsSql)]
    [TestFixture(typeof(EntityFrameworkViewManagerFactory), Category = TestCategories.MsSql, Ignore = true, IgnoreReason = "The contained List<int> cannot be persisted by EF")]
    [TestFixture(typeof(InMemoryViewManagerFactory))]
    public class RecoveryTest<TFactory> : FixtureBase where TFactory : AbstractViewManagerFactory, new()
    {
        TFactory _factory;
        TestContext _context;
        IViewManager<View> _viewManager;

        protected override void DoSetUp()
        {
            CirqusLoggerFactory.Current = new ConsoleLoggerFactory(minLevel: Logger.Level.Warn);

            _factory = RegisterForDisposal(new TFactory());
            _viewManager = _factory.GetViewManager<View>();

            _context = RegisterForDisposal(
                TestContext.With()
                    .EventDispatcher(x => x.UseViewManagerEventDispatcher(_viewManager).WithMaxDomainEventsPerBatch(10))
                    .Options(x => x.Asynchronous())
                    .Create());
        }

        [TestCase(100, 1.2, 0.1)]
        [TestCase(1000, 5, 0.3)]
        [Description("Saves a bunch of events and kicks of a timer that will force the view to fail at regular intervals, checking whether the view always ends up processing all the events as it should")]
        public void CanRecoverAfterTransientErrors(int eventCount, double failIntervalSeconds, double failDurationSeconds)
        {
            // arrange
            var eventIds = Enumerable.Range(0, eventCount).ToList();
            var failCount = 0;

            using (var failTimer = new Timer(TimeSpan.FromSeconds(failIntervalSeconds).TotalMilliseconds))
            {
                failTimer.Elapsed += delegate
                {
                    View.Fail = true;
                    failCount++;

                    ThreadPool.QueueUserWorkItem(_ =>
                    {
                        Thread.Sleep(TimeSpan.FromSeconds(failDurationSeconds));
                        View.Fail = false;
                    });
                };
                failTimer.Start();


                var events = eventIds
                    .Select(id => new Event { EventId = id })
                    .ToList();

                events.ForEach(e => _context.Save("someid", e));

                // act
                _context.AddViewManager(_viewManager);
                _context.WaitForViewsToCatchUp(300);
            }

            Console.WriteLine(@"====================================================
Processed {0} events
Failed every {1} seconds (total fail count: {2})
====================================================", eventCount, failIntervalSeconds, failCount);

            // assert
            var viewInstance = _viewManager.Load(GlobalInstanceLocator.GetViewInstanceId());

            Assert.That(viewInstance.EventIds, Is.EqualTo(eventIds));
        }
    }
}
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using d60.Cirqus.Logging;
using d60.Cirqus.Logging.Console;
using d60.Cirqus.MongoDb;
using d60.Cirqus.MongoDb.Config;
using d60.Cirqus.MongoDb.Views;
using d60.Cirqus.Tests.MongoDb;
using d60.Cirqus.Tests.Snapshotting.Models;
using d60.Cirqus.Views;
using MongoDB.Driver;
using NUnit.Framework;

namespace d60.Cirqus.Tests.Snapshotting
{
    [TestFixture, Category(TestCategories.MongoDb)]
    public class TestNewNewSnapshotting : FixtureBase
    {
        MongoDatabase _database;

        protected override void DoSetUp()
        {
            _database = MongoHelper.InitializeTestDatabase();
            CirqusLoggerFactory.Current = new ConsoleLoggerFactory(Logger.Level.Warn);
        }

        [TestCase(false, 200)]
        [TestCase(true, 10000)]
        public void RunTest(bool enableSnapshotting, int numberOfCommandsToProcess)
        {
            var handleTimes = new ConcurrentQueue<DispatchStats>();
            var viewManager = CreateViewManager();
            var commandProcessor = CreateCommandProcessor(enableSnapshotting, viewManager, handleTimes);

            var stopwatch = Stopwatch.StartNew();

            var lastResult = Enumerable.Range(0, numberOfCommandsToProcess)
                .Select(i => commandProcessor.ProcessCommand(new IncrementRoot("bimse!")))
                .Last();

            viewManager.WaitUntilProcessed(lastResult, TimeSpan.FromMinutes(2)).Wait();

            Console.WriteLine();
            Console.WriteLine("Processing {0} commands took {1:0.0} s in total", numberOfCommandsToProcess, stopwatch.Elapsed.TotalSeconds);
            Console.WriteLine();

            var maxTime = handleTimes.Max(t => t.Elapsed);

            var statsLines = string.Join(Environment.NewLine, handleTimes
                .GroupBy(l => RoundToSeconds(l, TimeSpan.FromSeconds(10)))
                .Select(g => new DispatchStats(g.Key, TimeSpan.FromSeconds(g.Average(e => e.Elapsed.TotalSeconds))))
                .Select(time =>
                {
                    var timeString = time.Elapsed.TotalSeconds.ToString("0.00").PadLeft(8);
                    var bar = new string('=', (int)(100.0 * (time.Elapsed.TotalSeconds / maxTime.TotalSeconds)));

                    return string.Concat(timeString, ": ", bar);
                }));

            Console.WriteLine(statsLines);
            Console.WriteLine("0.00 - {0:0.00} s", maxTime.TotalSeconds);
        }

        DateTime RoundToSeconds(DispatchStats dispatchStats, TimeSpan resolution)
        {
            var oneSecond = resolution.Ticks;
            var number = dispatchStats.Time.Ticks/oneSecond;
            return new DateTime(number*oneSecond);
        }

        IViewManager CreateViewManager()
        {
            return new MongoDbViewManager<RootNumberView>(_database);
        }

        ICommandProcessor CreateCommandProcessor(bool enableSnapshotting, IViewManager viewManager, ConcurrentQueue<DispatchStats> handleTimes)
        {
            return CommandProcessor.With()
                .EventStore(e => e.UseMongoDb(_database, "Events"))
                .EventDispatcher(e =>
                {
                    var items = new Dictionary<string, object> { { "stats", handleTimes } };

                    e.UseViewManagerEventDispatcher(viewManager)
                        .WithViewContext(items);
                })
                .Options(o =>
                {
                    if (enableSnapshotting)
                    {
                        Console.WriteLine("Enabling snapshotting");

                        o.EnableSnapshotting(_database, "Snapshots");
                    }
                })
                .Create();
        }

        public class DispatchStats
        {
            public DateTime Time { get; private set; }
            public TimeSpan Elapsed { get; private set; }

            public DispatchStats(DateTime time, TimeSpan elapsed)
            {
                Time = time;
                Elapsed = elapsed;
            }
        }
    }
}
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using d60.Cirqus.Config;
using d60.Cirqus.Events;
using d60.Cirqus.Logging;
using d60.Cirqus.Logging.Console;
using d60.Cirqus.MongoDb.Config;
using d60.Cirqus.MongoDb.Events;
using d60.Cirqus.MongoDb.Snapshotting;
using d60.Cirqus.MongoDb.Views;
using d60.Cirqus.Numbers;
using d60.Cirqus.Serialization;
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

        [TestCase(false, 1000)]
        [TestCase(true, 1000)]
        public void RunCommandProcessingTest(bool enableSnapshotting, int numberOfCommandsToProcess)
        {
            var handleTimes = new ConcurrentQueue<DispatchStats>();
            var viewManager = CreateViewManager();
            var commandProcessor = CreateCommandProcessor(enableSnapshotting, viewManager, handleTimes);

            var stopwatch = Stopwatch.StartNew();

            var lastResult = Enumerable.Range(0, numberOfCommandsToProcess)
                .Select(i => commandProcessor.ProcessCommand(new IncrementRoot("bimse!")))
                .Last();

            Console.WriteLine("Waiting for views to catch up to {0}", lastResult);
            viewManager.WaitUntilProcessed(lastResult, TimeSpan.FromMinutes(5)).Wait();

            Console.WriteLine();
            Console.WriteLine("Processing {0} commands took {1:0.0} s in total", numberOfCommandsToProcess, stopwatch.Elapsed.TotalSeconds);
            Console.WriteLine();

            var stats = handleTimes
                .GroupBy(l => RoundToSeconds(l, TimeSpan.FromSeconds(10)))
                .Select(g => new DispatchStats(g.Key, TimeSpan.FromSeconds(g.Average(e => e.Elapsed.TotalSeconds))))
                .ToList();

            var maxTime = stats.Max(t => t.Elapsed);

            var statsLines = string.Join(Environment.NewLine, stats
                .Select(time =>
                {
                    var timeString = time.Elapsed.TotalSeconds.ToString("0.00").PadLeft(8);
                    var bar = new string('=', (int)(100.0 * (time.Elapsed.TotalSeconds / maxTime.TotalSeconds)));

                    return string.Concat(timeString, ": ", bar);
                }));

            Console.WriteLine(statsLines);
            Console.WriteLine("0.00 - {0:0.00} s", maxTime.TotalSeconds);
        }

        [TestCase(false, 5000)]
        [TestCase(true, 5000)]
        public void RunEventReplayingTest(bool enableSnapshotting, int numberOfCommandsToProcess)
        {
            SaveEvents(numberOfCommandsToProcess, "bimse!");

            var handleTimes = new ConcurrentQueue<DispatchStats>();
            var viewManager = CreateViewManager();
            CreateCommandProcessor(enableSnapshotting, viewManager, handleTimes);

            var stopwatch = Stopwatch.StartNew();

            var lastResult = GetLastResult();

            Console.WriteLine("Waiting for views to catch up to {0}", lastResult);
            viewManager.WaitUntilProcessed(lastResult, TimeSpan.FromMinutes(5)).Wait();

            Console.WriteLine();
            Console.WriteLine("Processing {0} events took {1:0.0} s in total", numberOfCommandsToProcess, stopwatch.Elapsed.TotalSeconds);
            Console.WriteLine();

            var stats = handleTimes
                .GroupBy(l => RoundToSeconds(l, TimeSpan.FromSeconds(1)))
                .Select(g => new DispatchStats(g.Key, TimeSpan.FromSeconds(g.Average(e => e.Elapsed.TotalSeconds))))
                .ToList();

            var maxTime = stats.Max(t => t.Elapsed);

            var statsLines = string.Join(Environment.NewLine, stats
                .Select(time =>
                {
                    var timeString = time.Elapsed.TotalSeconds.ToString("0.00").PadLeft(8);
                    var bar = new string('=', (int)(100.0 * (time.Elapsed.TotalSeconds / maxTime.TotalSeconds)));

                    return string.Concat(timeString, ": ", bar);
                }));

            Console.WriteLine(statsLines);
            Console.WriteLine("0.00 - {0:0.00} s", maxTime.TotalSeconds);
        }

        CommandProcessingResult GetLastResult()
        {
            var eventStore = new MongoDbEventStore(_database, "Events");
            var nextGlobalSequenceNumber = eventStore.GetNextGlobalSequenceNumber();
            var lastGlobalSequenceNumber = nextGlobalSequenceNumber - 1;
            return CommandProcessingResult.WithNewPosition(lastGlobalSequenceNumber);
        }

        void SaveEvents(int numberOfCommandsToProcess, string aggregateRootId)
        {
            var eventStore = new MongoDbEventStore(_database, "Events");
            var serializer = new JsonDomainEventSerializer();
            var typeNameMapper = new DefaultDomainTypeNameMapper();

            Enumerable.Range(0, numberOfCommandsToProcess)
                .ToList()
                .ForEach(number =>
                {
                    var domainEvents = new[]
                    {
                        new RootGotNewNumber(number)
                        {
                            Meta =
                            {
                                {DomainEvent.MetadataKeys.AggregateRootId, aggregateRootId},
                                {DomainEvent.MetadataKeys.SequenceNumber, number.ToString()},
                                {DomainEvent.MetadataKeys.TimeUtc, Time.UtcNow().ToString("u")},
                                {DomainEvent.MetadataKeys.Type, typeNameMapper.GetName(typeof(RootGotNewNumber))},
                                {DomainEvent.MetadataKeys.Owner, typeNameMapper.GetName(typeof(Root))},
                            }
                        }
                    };

                    eventStore.Save(Guid.NewGuid(), domainEvents.Select(e => serializer.Serialize(e)));
                });
        }

        DateTime RoundToSeconds(DispatchStats dispatchStats, TimeSpan resolution)
        {
            var oneSecond = resolution.Ticks;
            var number = dispatchStats.Time.Ticks / oneSecond;
            return new DateTime(number * oneSecond);
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

                        o.EnableExperimentalMongoDbSnapshotting(_database, "Snapshots");
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
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using d60.Cirqus.Aggregates;
using d60.Cirqus.Commands;
using d60.Cirqus.Config;
using d60.Cirqus.Config.Configurers;
using d60.Cirqus.Events;
using d60.Cirqus.Extensions;
using d60.Cirqus.Logging;
using d60.Cirqus.Logging.Console;
using d60.Cirqus.MongoDb.Config;
using d60.Cirqus.MongoDb.Views;
using d60.Cirqus.MsSql.Config;
using d60.Cirqus.MsSql.Views;
using d60.Cirqus.PostgreSql.Config;
using d60.Cirqus.Tests.MongoDb;
using d60.Cirqus.Tests.MsSql;
using d60.Cirqus.Tests.PostgreSql;
using d60.Cirqus.Views;
using d60.Cirqus.Views.ViewManagers;
using d60.Cirqus.Views.ViewManagers.Locators;
using MongoDB.Driver;
using NUnit.Framework;
using Timer = System.Timers.Timer;

namespace d60.Cirqus.Tests.Caching
{
    [TestFixture, Category(TestCategories.MongoDb), Description("Intentionally written as a O(n^2) in order to verify that event caching can help")]
    public class TestEventCaching : FixtureBase
    {
        [Flags]
        public enum CachingOption
        {
            None,
            EventCaching,
            AggregateRootSnapshotting
        }

        public enum PersistenceOption
        {
            MongoDb,
            PostgreSql,
            MsSql
        }

        const int NumberOfCommands = 500;

        [TestCase(CachingOption.None, PersistenceOption.MongoDb, NumberOfCommands)]
        [TestCase(CachingOption.EventCaching, PersistenceOption.MongoDb, NumberOfCommands, Category = TestCategories.MongoDb)]
        [TestCase(CachingOption.EventCaching, PersistenceOption.PostgreSql, NumberOfCommands, Category = TestCategories.PostgreSql)]
        [TestCase(CachingOption.EventCaching, PersistenceOption.MsSql, NumberOfCommands, Category = TestCategories.MsSql)]
        public async Task CheckPerformance(CachingOption cachingOption, PersistenceOption persistenceOption, int numberOfCommands)
        {
            CleanUp(persistenceOption);

            CirqusLoggerFactory.Current = new ConsoleLoggerFactory(minLevel: Logger.Level.Warn);

            var database = MongoHelper.InitializeTestDatabase();
            var waitHandle = new ViewManagerWaitHandle();

            var commandProcessor = CommandProcessor.With()
                .EventStore(e => ConfigureEventStore(persistenceOption, e, database))
                .AggregateRootRepository(x => ConfigureAggregateRootRepository(cachingOption, x))
                .EventDispatcher(e => ConfigureEventDispatcher(database, e, waitHandle, persistenceOption))
                .Options(x => ConfigureOptions(cachingOption, x))
                .Create();

            using (commandProcessor)
            using (var timer = new Timer(1000))
            {
                var numberOfCommandsProcessed = 0;

                timer.Elapsed += (o, ea) =>
                {
                    var numberOfCommandsProcessedSinceLastTick = Interlocked.Exchange(ref numberOfCommandsProcessed, 0);
                    var numberOfEventsPocessedSinceLastTick = Interlocked.Exchange(ref View.NumberOfEventsProcessed, 0);

                    if (numberOfCommandsProcessedSinceLastTick > 0 || numberOfEventsPocessedSinceLastTick > 0)
                    {
                        Console.WriteLine("{0} {1}",
                            numberOfCommandsProcessedSinceLastTick.ToString().PadLeft(3),
                            new string('C', numberOfCommandsProcessedSinceLastTick));

                        Console.WriteLine("{0} {1}",
                            numberOfEventsPocessedSinceLastTick.ToString().PadLeft(3),
                            new string('E', numberOfEventsPocessedSinceLastTick));
                    }
                };

                timer.Start();

                Console.WriteLine("Processing {0} commands", numberOfCommands);

                var stopwatch = Stopwatch.StartNew();
                var lastResult = Enumerable.Range(0, numberOfCommands)
                    .Select(number =>
                    {
                        var result = commandProcessor.ProcessCommand(new Command("bimse!"));
                        Interlocked.Increment(ref numberOfCommandsProcessed);
                        return result;
                    })
                    .Last();

                await waitHandle.WaitForAll(lastResult, TimeSpan.FromMinutes(1));

                Console.WriteLine("Done! - elapsed seconds: {0:0.0}", stopwatch.Elapsed.TotalSeconds);
            }
        }

        void CleanUp(PersistenceOption persistenceOption)
        {
            switch (persistenceOption)
            {
                case PersistenceOption.PostgreSql:
                    PostgreSqlTestHelper.DropTable("Events");
                    MsSqlTestHelper.DropTable("View");
                    MsSqlTestHelper.DropTable("View_Position");
                    break;
                case PersistenceOption.MsSql:
                    MsSqlTestHelper.DropTable("Events");
                    MsSqlTestHelper.DropTable("View");
                    MsSqlTestHelper.DropTable("View_Position");
                    break;
                case PersistenceOption.MongoDb:
                    // nothing to clean up
                    break;
                default:
                    throw new ArgumentOutOfRangeException("persistenceOption");
            }
        }

        static void ConfigureEventDispatcher(MongoDatabase database, EventDispatcherConfigurationBuilder e, ViewManagerWaitHandle waitHandle, PersistenceOption persistenceOption)
        {
            IViewManager viewManager;

            switch (persistenceOption)
            {
                case PersistenceOption.MongoDb:
                    viewManager = new MongoDbViewManager<View>(database);
                    break;
                case PersistenceOption.PostgreSql:
                    viewManager = new MsSqlViewManager<View>(MsSqlTestHelper.ConnectionString);
                    break;
                case PersistenceOption.MsSql:
                    viewManager = new MsSqlViewManager<View>(MsSqlTestHelper.ConnectionString);
                    break;
                default:
                    throw new ArgumentOutOfRangeException("persistenceOption", string.Format("Unknown value: {0}", persistenceOption));
            }

            e.UseViewManagerEventDispatcher(waitHandle, viewManager);
        }

        static void ConfigureOptions(CachingOption cachingOption, OptionsConfigurationBuilder x)
        {
            if ((cachingOption & CachingOption.EventCaching) == CachingOption.EventCaching)
            {
                Console.WriteLine("Enabling event caching");
                x.EnableEventCaching(10000);
            }
        }

        static void ConfigureAggregateRootRepository(CachingOption cachingOption, AggregateRootRepositoryConfigurationBuilder x)
        {
            if ((cachingOption & CachingOption.AggregateRootSnapshotting) == CachingOption.AggregateRootSnapshotting)
            {
                Console.WriteLine("Enabling aggregate root snapshots");
                x.EnableInMemorySnapshotCaching(1);
            }
        }

        static void ConfigureEventStore(PersistenceOption persistenceOption, EventStoreConfigurationBuilder e,
            MongoDatabase database)
        {
            switch (persistenceOption)
            {
                case PersistenceOption.MongoDb:
                    e.UseMongoDb(database, "Events");
                    break;
                case PersistenceOption.PostgreSql:
                    e.UsePostgreSql(PostgreSqlTestHelper.PostgreSqlConnectionString, "Events");
                    break;
                case PersistenceOption.MsSql:
                    e.UseSqlServer(MsSqlTestHelper.ConnectionString, "Events");
                    break;
                default:
                    throw new ArgumentOutOfRangeException("persistenceOption",
                        string.Format("Unknown value: {0}", persistenceOption));
            }
        }

        class Root : AggregateRoot, IEmit<Event>
        {
            public int EventsEmitted { get; set; }

            public void DoYourThing()
            {
                Emit(new Event { EventNumber = EventsEmitted + 1 });
            }

            public void Apply(Event e)
            {
                EventsEmitted = e.EventNumber;
            }
        }

        class Event : DomainEvent<Root>
        {
            public int EventNumber { get; set; }
        }

        class Command : Command<Root>
        {
            public Command(string aggregateRootId)
                : base(aggregateRootId)
            {
            }

            public override void Execute(Root aggregateRoot)
            {
                aggregateRoot.DoYourThing();
            }
        }

        class View : IViewInstance<InstancePerAggregateRootLocator>, ISubscribeTo<Event>
        {
            public static int NumberOfEventsProcessed;

            public string Id { get; set; }

            public long LastGlobalSequenceNumber { get; set; }

            public int EventsEmitted { get; set; }

            public void Handle(IViewContext context, Event domainEvent)
            {
                var instance = context.Load<Root>(domainEvent.GetAggregateRootId());

                EventsEmitted = instance.EventsEmitted;

                Interlocked.Increment(ref NumberOfEventsProcessed);
            }
        }
    }
}
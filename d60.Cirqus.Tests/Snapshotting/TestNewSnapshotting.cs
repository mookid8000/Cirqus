using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Authentication.ExtendedProtection.Configuration;
using System.Threading.Tasks;
using d60.Cirqus.Aggregates;
using d60.Cirqus.Commands;
using d60.Cirqus.Events;
using d60.Cirqus.Extensions;
using d60.Cirqus.Logging;
using d60.Cirqus.MongoDb.Config;
using d60.Cirqus.MongoDb.Views;
using d60.Cirqus.Snapshotting;
using d60.Cirqus.Tests.MongoDb;
using d60.Cirqus.Views.ViewManagers;
using d60.Cirqus.Views.ViewManagers.Locators;
using MongoDB.Driver;
using NUnit.Framework;

namespace d60.Cirqus.Tests.Snapshotting
{
    [TestFixture, Category(TestCategories.MongoDb)]
    public class TestNewSnapshotting : FixtureBase
    {
        static readonly MongoDatabase _database;

        static TestNewSnapshotting()
        {
            _database = MongoHelper.InitializeTestDatabase();
        }

        [TestCase(true, 0)]
        [TestCase(false, 0)]
        [TestCase(true, 1)]
        [TestCase(false, 1)]
        public async Task RealisticScenario(bool enableSnapshotting, int randomNumber)
        {
            var description = string.Format("snapshotting {0}", enableSnapshotting ? "enabled" : "disabled");

            await TakeTimeAsync(description, () => Run(enableSnapshotting));
        }

        async Task Run(bool enableSnapshotting)
        {
            _database.Drop();

            var waitHandle = new ViewManagerWaitHandle();
            var commandProcessor = CommandProcessor.With()
                .Logging(l => l.UseConsole(minLevel:Logger.Level.Warn))
                .EventStore(e => e.UseMongoDb(_database, "Events"))
                .EventDispatcher(e => e.UseViewManagerEventDispatcher(new MongoDbViewManager<DemandingView>(_database)).WithWaitHandle(waitHandle))
                .AggregateRootRepository(e =>
                {
                    if (enableSnapshotting)
                    {
                        e.Decorate(c => new NewSimpleSnapshottingAggregateRootRepositoryDecorator(c.Get<IAggregateRootRepository>()));
                    }
                })
                .Create();

            var ids = Enumerable.Range(0, 10)
                .Select(i => string.Format("id{0}", i))
                .ToArray();

            using (commandProcessor)
            {
                var lastResult = Enumerable.Range(0, 10)
                    .Select(i => commandProcessor.ProcessCommand(new Command(ids)))
                    .Last();

                await waitHandle.WaitForAll(lastResult, TimeSpan.FromMinutes(2));
            }
        }

        public class DemandingView : IViewInstance<GlobalInstanceLocator>, ISubscribeTo<Event>
        {
            public DemandingView()
            {
                AggregateRootIds = new HashSet<string>();
                EventsForEachRoot = new Dictionary<string, int>();
            }
            public string Id { get; set; }
            public long LastGlobalSequenceNumber { get; set; }
            public HashSet<string> AggregateRootIds { get; set; }
            public Dictionary<string, int> EventsForEachRoot { get; set; }
            public void Handle(IViewContext context, Event domainEvent)
            {
                AggregateRootIds.Add(domainEvent.GetAggregateRootId());

                foreach (var root in AggregateRootIds.Select(id => context.Load<Root>(id)))
                {
                    EventsForEachRoot[root.Id] = root.EventsProcessed;
                }
            }
        }

        public class Command : ExecutableCommand
        {
            public Command(string[] ids)
            {
                Ids = ids;
            }

            public string[] Ids { get; private set; }

            public override void Execute(ICommandContext context)
            {
                foreach (var id in Ids)
                {
                    var root = context.TryLoad<Root>(id)
                               ?? context.Create<Root>(id);
                    
                    root.Act();
                }
            }
        }

        public class Root : AggregateRoot, IEmit<Event>
        {
            int _eventNumber;

            public void Act()
            {
                Emit(new Event {Number = _eventNumber + 1});
            }
            
            public void Apply(Event e)
            {
                _eventNumber = e.Number;
            }

            public int EventsProcessed
            {
                get { return _eventNumber; }
            }
        }

        public class Event : DomainEvent<Root>
        {
            public int Number { get; set; }
        }
    }
}
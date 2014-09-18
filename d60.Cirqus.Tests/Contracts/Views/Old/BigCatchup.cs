using System;
using System.Collections.Generic;
using System.Linq;
using d60.Cirqus.Events;
using d60.Cirqus.Extensions;
using d60.Cirqus.Logging;
using d60.Cirqus.Logging.Console;
using d60.Cirqus.MongoDb.Events;
using d60.Cirqus.Projections.Views.ViewManagers;
using d60.Cirqus.Projections.Views.ViewManagers.Locators;
using d60.Cirqus.Projections.Views.ViewManagers.Old;
using d60.Cirqus.Tests.Contracts.Views.Old.Factories;
using d60.Cirqus.Tests.MongoDb;
using d60.Cirqus.Tests.Stubs;
using MongoDB.Driver;
using NUnit.Framework;

namespace d60.Cirqus.Tests.Contracts.Views.Old
{
    [TestFixture(typeof(MongoDbPullViewManagerFactory), Category = TestCategories.MongoDb)]
    [TestFixture(typeof(MsSqlPullViewManagerFactory), Category = TestCategories.MsSql)]
    [TestFixture(typeof(EntityFrameworkPullViewManagerFactory), Category = TestCategories.MsSql)]
    public class BigCatchup<TViewManagerFactory> : FixtureBase where TViewManagerFactory : IPullViewManagerFactory, new()
    {
        MongoDatabase _database;
        MongoDbEventStore _eventStore;

        IPullViewManager _viewManager;
        TViewManagerFactory _factory;

        protected override void DoSetUp()
        {
            CirqusLoggerFactory.Current = new ConsoleLoggerFactory(minLevel: Logger.Level.Info);

            _database = MongoHelper.InitializeTestDatabase();
            _eventStore = new MongoDbEventStore(_database, "events");

            _factory = new TViewManagerFactory();
            _viewManager = _factory.GetPullViewManager<JustAnotherViewInstance>();
        }

        [TestCase(4, 2, 1)]
        //[TestCase(1000, 100, 10)]
        //[TestCase(100000, 1000, 20, Ignore = true)]
        //[TestCase(1000000, 10000, 40, Ignore = true)]
        public void GenerateFreshView(int numberOfEvents, int numberOfAggregateRoots, int uowSize)
        {
            var random = new Random(DateTime.Now.GetHashCode());
            var aggregateRootIds = Enumerable.Range(0, numberOfAggregateRoots).Select(i => Guid.NewGuid()).ToArray();
            var nextSeqNoById = new Dictionary<Guid, long>();

            Func<Guid, long> getNextSequenceNumberFor = id =>
            {
                if (!nextSeqNoById.ContainsKey(id))
                {
                    nextSeqNoById[id] = 0;
                }

                return nextSeqNoById[id]++;
            };
            Func<Guid> getRandomAggregateRootId = () => aggregateRootIds[random.Next(aggregateRootIds.Length)];

            Console.WriteLine("Saving {0} events distributed among {1} roots", numberOfEvents, numberOfAggregateRoots);

            var savedEventsCount = 0;

            var batches = Enumerable.Range(0, numberOfEvents)
                .Select(i =>
                {
                    var id = getRandomAggregateRootId();
                    var seqNo = getNextSequenceNumberFor(id);

                    return EventFor(id, seqNo);
                })
                .Batch(uowSize)
                .ToList();

            TakeTime("Save " + numberOfEvents + " events", () =>
            {
                foreach (var batch in batches)
                {
                    var events = batch.ToList();

                    _eventStore.Save(Guid.NewGuid(), events);

                    savedEventsCount += events.Count;
                }
            }, elapsed => Console.WriteLine("Events saved: {0} ({1:0} events/s)", savedEventsCount, savedEventsCount / elapsed.TotalSeconds));

            Console.WriteLine("Done - initiating catch-up");

            TakeTime("Catch-up involving " + numberOfEvents + " events", () => _viewManager.Initialize(new ThrowingViewContext(), _eventStore));

            foreach (var id in aggregateRootIds)
            {
                if (!nextSeqNoById.ContainsKey(id))
                {
                    Console.WriteLine("Didn't create event(s) for {0} - skipping!", id);
                    continue;
                }

                Console.WriteLine("Checking seq no for {0}...", id);
                var viewIdFromAggregateRootId = InstancePerAggregateRootLocator.GetViewIdFromAggregateRootId(id);
                var view = _factory.Load<JustAnotherViewInstance>(viewIdFromAggregateRootId);

                Assert.That(view, Is.Not.Null, "Could not find view for ID {0}   !!", id);
                Assert.That(view.EventCounter, Is.EqualTo(nextSeqNoById[id]), "Event counter did not yield a number that corresponds to the right place in the root's sequence");
            }
        }

        DomainEvent EventFor(Guid aggregateRootId, long seqNo)
        {
            return new AnEventMore
            {
                Meta =
                {
                    {DomainEvent.MetadataKeys.AggregateRootId, aggregateRootId},
                    {DomainEvent.MetadataKeys.SequenceNumber, seqNo},
                },
                SomeData = new string('*', 1024)
            };
        }
    }

    class JustAnotherViewInstance : IViewInstance<InstancePerAggregateRootLocator>, ISubscribeTo<AnEventMore>
    {
        public int EventCounter { get; set; }

        public Guid AggregateRootId { get; set; }

        public void Handle(IViewContext context, AnEventMore domainEvent)
        {
            AggregateRootId = domainEvent.GetAggregateRootId();
            EventCounter++;

            Console.WriteLine("Counter is now {0} for {1} (global: {2})", EventCounter, AggregateRootId, domainEvent.GetGlobalSequenceNumber());
        }

        public string Id { get; set; }

        public long LastGlobalSequenceNumber { get; set; }
    }

    class AnEventMore : DomainEvent
    {
        public string SomeData { get; set; }
    }
}
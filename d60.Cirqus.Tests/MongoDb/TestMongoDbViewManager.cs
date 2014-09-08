using System;
using System.Collections.Generic;
using System.Linq;
using d60.Cirqus.Config;
using d60.Cirqus.Events;
using d60.Cirqus.Extensions;
using d60.Cirqus.Logging;
using d60.Cirqus.Logging.Console;
using d60.Cirqus.MongoDb.Events;
using d60.Cirqus.MongoDb.Views;
using d60.Cirqus.MongoDb.Views.Old;
using d60.Cirqus.Tests.Stubs;
using d60.Cirqus.Views.ViewManagers;
using d60.Cirqus.Views.ViewManagers.Locators;
using MongoDB.Driver;
using NUnit.Framework;

namespace d60.Cirqus.Tests.MongoDb
{
    [TestFixture, Category(TestCategories.MongoDb)]
    public class TestMongoDbViewManager : FixtureBase
    {
        readonly Random _random = new Random();

        MongoDatabase _database;
        MongoDbViewManager<MyView> _viewManager;
        MongoDbEventStore _eventStore;
        Guid[] _aggregateRootIds;
        Dictionary<Guid, long> _sequenceNumbers;

        protected override void DoSetUp()
        {
            CirqusLoggerFactory.Current = new ConsoleLoggerFactory(minLevel: Logger.Level.Warn);

            _aggregateRootIds = Enumerable.Range(0, 1000).Select(i => Guid.NewGuid()).ToArray();

            _database = MongoHelper.InitializeTestDatabase();
            _eventStore = new MongoDbEventStore(_database, "events");
            _sequenceNumbers = new Dictionary<Guid, long>();

            _viewManager = new MongoDbViewManager<MyView>(_database, "views")
            {
                MaxDomainEventsBetweenFlush = 10
            };
        }

        Guid GetRandomAggregateRootId()
        {
            return _aggregateRootIds[_random.Next(_aggregateRootIds.Length)];
        }

        long GetNextSequenceNumberFor(Guid aggregateRootId)
        {
            if (!_sequenceNumbers.ContainsKey(aggregateRootId))
                _sequenceNumbers[aggregateRootId] = 0;

            return _sequenceNumbers[aggregateRootId]++;
        }

        [Test]
        public void CannotEndInInconsistentState()
        {
            _viewManager.Initialize(new ThrowingViewContext(), _eventStore);

            const int lastGlobalSequenceNumber = 10000;

            var batches = Enumerable
                .Range(0, lastGlobalSequenceNumber)
                .Select(i =>
                {
                    var aggregateRootId = GetRandomAggregateRootId();

                    return new AnEvent
                                 {
                                     Meta =
                                     {
                                         {DomainEvent.MetadataKeys.GlobalSequenceNumber, i},
                                         {DomainEvent.MetadataKeys.AggregateRootId, aggregateRootId},
                                         {DomainEvent.MetadataKeys.SequenceNumber, GetNextSequenceNumberFor(aggregateRootId)},
                                     }
                                 };
                })
                .Batch(10);

            foreach (var batch in batches)
            {
                _eventStore.Save(Guid.NewGuid(), batch);
            }

            new Retryer().RetryOn<ApplicationException>(() => _viewManager.CatchUp(new ThrowingViewContext(), _eventStore, lastGlobalSequenceNumber),
                maxRetries:100);

            var viewsCollection = _database.GetCollection<MyView>("views");
            var allViews = viewsCollection.FindAll().ToList();

            Assert.That(allViews.Sum(v => v.EventCounter), Is.EqualTo(lastGlobalSequenceNumber));
        }

        class MyView : IViewInstance<InstancePerAggregateRootLocator>, ISubscribeTo<AnEvent>
        {
            static readonly Random Randomizzle = new Random();

            public string Id { get; set; }

            public long LastGlobalSequenceNumber { get; set; }

            public int EventCounter { get; set; }

            public void Handle(IViewContext context, AnEvent domainEvent)
            {
                EventCounter++;

                if (Randomizzle.Next(1000) != 5) return;

                Console.Write("!");
                throw new ApplicationException("bummer dude!");
            }
        }

        class AnEvent : DomainEvent
        {
        }
    }
}
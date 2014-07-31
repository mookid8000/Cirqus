using System;
using System.Collections.Generic;
using System.Linq;
using d60.EventSorcerer.Events;
using d60.EventSorcerer.Extensions;
using d60.EventSorcerer.MongoDb.Events;
using d60.EventSorcerer.MongoDb.Views;
using d60.EventSorcerer.Views.Basic;
using d60.EventSorcerer.Views.Basic.Locators;
using MongoDB.Driver;
using NUnit.Framework;

namespace d60.EventSorcerer.Tests.MongoDb.Views
{
    [TestFixture, Ignore]
    [Category(TestCategories.MongoDb)]
    public class TestMongoDbCatchUpViewManager_BigCatchup : FixtureBase
    {
        MongoDatabase _database;
        MongoDbCatchUpViewManager<JustAnotherView> _viewManager;
        MongoDbEventStore _eventStore;

        protected override void DoSetUp()
        {
            _database = Helper.InitializeTestDatabase();
            _eventStore = new MongoDbEventStore(_database, "events");
            _viewManager = new MongoDbCatchUpViewManager<JustAnotherView>(_database, "justAnother");
        }


        [TestCase(1000, 100)]
        public void CanCatchUpIfEventStoreAllowsIt(int numberOfEvents, int numberOfAggregateRoots)
        {
            var random = new Random(DateTime.Now.GetHashCode());
            var aggregateRootIds = Enumerable.Range(0, numberOfAggregateRoots).Select(i => Guid.NewGuid()).ToArray();
            var seqNos = new Dictionary<Guid, long>();
            
            Func<Guid, long> getNextSequenceNumberFor = id =>
            {
                if (!seqNos.ContainsKey(id)) seqNos[id] = 0;

                return seqNos[id]++;
            };
            Func<Guid> getRandomAggregateRootId = () => aggregateRootIds[random.Next(aggregateRootIds.Length)];

            Console.WriteLine("Saving {0} events distributed among {1} roots", numberOfEvents, numberOfAggregateRoots);

            numberOfEvents.Times(() =>
            {
                var id = getRandomAggregateRootId();
                var seqNo = getNextSequenceNumberFor(id);

                _eventStore.Save(Guid.NewGuid(), new[] { EventFor(id, seqNo) });
            });

            Console.WriteLine("Done - initiating catch-up");
            
            TakeTime("Catch-up involving " + numberOfEvents + " events", () => _viewManager.Initialize(_eventStore));

            foreach (var id in aggregateRootIds)
            {
                var view = _viewManager.Load(InstancePerAggregateRootLocator.GetViewIdFromGuid(id));

                Assert.That(view.EventCounter, Is.EqualTo(seqNos[id]));
            }
        }

        DomainEvent EventFor(Guid aggregateRootId, long seqNo)
        {
            return new AnEvent
            {
                Meta =
                {
                    {DomainEvent.MetadataKeys.AggregateRootId, aggregateRootId},
                    {DomainEvent.MetadataKeys.SequenceNumber, seqNo},
                },
                SomeData = new string('*', 1024)
            };
        }

        class JustAnotherView : IView<InstancePerAggregateRootLocator>, ISubscribeTo<AnEvent>
        {
            public int EventCounter { get; set; }
            public Guid AggregateRootId { get; set; }
            public void Handle(AnEvent domainEvent)
            {
                AggregateRootId = domainEvent.GetAggregateRootId();
                EventCounter++;
            }

            public string Id { get; set; }
        }

        class AnEvent : DomainEvent
        {
            public string SomeData { get; set; }
        }
    }
}
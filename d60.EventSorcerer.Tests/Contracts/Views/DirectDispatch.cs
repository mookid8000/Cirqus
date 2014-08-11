using System;
using System.Collections.Generic;
using System.Text;
using d60.EventSorcerer.Aggregates;
using d60.EventSorcerer.Events;
using d60.EventSorcerer.MongoDb.Events;
using d60.EventSorcerer.Numbers;
using d60.EventSorcerer.Tests.Aggregates;
using d60.EventSorcerer.Tests.Contracts.Views.Factories;
using d60.EventSorcerer.Tests.MongoDb;
using d60.EventSorcerer.Tests.Stubs;
using d60.EventSorcerer.Views.Basic;
using d60.EventSorcerer.Views.Basic.Locators;
using MongoDB.Driver;
using NUnit.Framework;

namespace d60.EventSorcerer.Tests.Contracts.Views
{
    [TestFixture(typeof(MongoDbPullViewManagerFactory), Category = TestCategories.MongoDb)]
    [TestFixture(typeof(MsSqlPullViewManagerFactory), Category = TestCategories.MsSql)]
    [TestFixture(typeof(EntityFrameworkPullViewManagerFactory), Category = TestCategories.MsSql)]
    public class DirectDispatch<TViewManagerFactory> : FixtureBase where TViewManagerFactory : IPushViewManagerFactory, new()
    {
        IPushViewManager _viewManager;
        TViewManagerFactory _viewManagerFactory;
        BasicEventDispatcher _eventDispatcher;
        MongoDatabase _database;
        MongoDbEventStore _eventStore;

        TestNumberGenerator _numberGenerator;

        protected override void DoSetUp()
        {
            _numberGenerator = new TestNumberGenerator();

            _database = MongoHelper.InitializeTestDatabase();
            _eventStore = new MongoDbEventStore(_database, "events");

            _viewManagerFactory = new TViewManagerFactory();

            _viewManager = _viewManagerFactory.GetPushViewManager<SomeView>();

            _eventDispatcher = new BasicEventDispatcher(new DefaultAggregateRootRepository(_eventStore), _viewManager);
        }

        [Test]
        public void CanDoDirectDispatchOfEvents()
        {
            var aggregateRootId = Guid.NewGuid();

            _eventDispatcher
                .Dispatch(_eventStore, new DomainEvent[]
                {
                    CreateEvent("hej", aggregateRootId),
                    CreateEvent("med", aggregateRootId),
                    CreateEvent("dig", aggregateRootId),
                });

            var view = _viewManagerFactory.Load<SomeView>(InstancePerAggregateRootLocator.GetViewIdFromGuid(aggregateRootId));
            var accumulatedData = view.CollectedData;
            
            Assert.That(view, Is.Not.Null);
            Assert.That(accumulatedData, Is.EqualTo("hej,med,dig"));
        }

        SomeEvent CreateEvent(string data, Guid aggregateRootId)
        {
            var someEvent = new SomeEvent
            {
                Data = data,
                Meta = {{DomainEvent.MetadataKeys.AggregateRootId, aggregateRootId}}
            }.NumberedWith(_numberGenerator);

            return someEvent;
        }
    }

    class SomeView : IViewInstance<InstancePerAggregateRootLocator>, ISubscribeTo<SomeEvent>
    {
        public string Id { get; set; }

        public long LastGlobalSequenceNumber { get; set; }

        public string CollectedData { get; set; }

        public void Handle(IViewContext context, SomeEvent domainEvent)
        {
            if (string.IsNullOrWhiteSpace(CollectedData))
            {
                CollectedData = domainEvent.Data;
                return;
            }

            CollectedData += "," + domainEvent.Data;
        }
    }

    class SomeEvent : DomainEvent<SomeRoot>
    {
        public string Data { get; set; }
    }

    class SomeRoot : AggregateRoot { }
}
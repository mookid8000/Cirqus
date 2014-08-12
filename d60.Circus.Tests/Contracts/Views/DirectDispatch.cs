using System;
using d60.Circus.Aggregates;
using d60.Circus.Events;
using d60.Circus.MongoDb.Events;
using d60.Circus.Tests.Contracts.Views.Factories;
using d60.Circus.Tests.MongoDb;
using d60.Circus.Tests.Stubs;
using d60.Circus.Views.ViewManagers;
using d60.Circus.Views.ViewManagers.Locators;
using MongoDB.Driver;
using NUnit.Framework;

namespace d60.Circus.Tests.Contracts.Views
{
    [TestFixture(typeof(MongoDbPullViewManagerFactory), Category = TestCategories.MongoDb)]
    [TestFixture(typeof(MsSqlPullViewManagerFactory), Category = TestCategories.MsSql)]
    [TestFixture(typeof(EntityFrameworkPullViewManagerFactory), Category = TestCategories.MsSql)]
    [TestFixture(typeof(InMemoryViewManagerFactory))]
    public class DirectDispatch<TViewManagerFactory> : FixtureBase where TViewManagerFactory : IPushViewManagerFactory, new()
    {
        IPushViewManager _viewManager;
        TViewManagerFactory _viewManagerFactory;
        ViewManagerEventDispatcher _eventDispatcher;
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

            _eventDispatcher = new ViewManagerEventDispatcher(new DefaultAggregateRootRepository(_eventStore), _viewManager);
        }

        [Test]
        public void CanDoDirectDispatchOfEvents()
        {
            _eventDispatcher.Initialize(_eventStore);

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

        [Test]
        public void DirectDispatchMustThrowAnErrorIfItHasNotBeenInitialized()
        {
            // deliberately do NOT initialize
            //_eventDispatcher.Initialize(_eventStore);

            _eventDispatcher.Dispatch(_eventStore, new DomainEvent[] { CreateEvent("hej", Guid.NewGuid())});

            Assert.That(_viewManager.Stopped, Is.True);
        }


        SomeEvent CreateEvent(string data, Guid aggregateRootId)
        {
            var someEvent = new SomeEvent
            {
                Data = data,
                Meta = { { DomainEvent.MetadataKeys.AggregateRootId, aggregateRootId } }
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
using System;
using System.Collections.Generic;
using d60.Cirqus.Aggregates;
using d60.Cirqus.Events;
using d60.Cirqus.Extensions;
using d60.Cirqus.Tests.Contracts.Views.Factories;
using d60.Cirqus.Views.ViewManagers;
using d60.Cirqus.Views.ViewManagers.Locators;
using NUnit.Framework;
using TestContext = d60.Cirqus.TestHelpers.TestContext;

namespace d60.Cirqus.Tests.Contracts.Views
{
    [TestFixture(typeof(MongoDbViewManagerFactory), Category = TestCategories.MongoDb)]
    [TestFixture(typeof(MsSqlViewManagerFactory), Category = TestCategories.MsSql)]
    [TestFixture(typeof(InMemoryViewManagerFactory))]
    [Description("View managers must raise the Updated event whenever a view instance is updated")]
    public class UpdatedEvent<TFactory> : FixtureBase where TFactory : AbstractViewManagerFactory, new()
    {
        TestContext _context;
        TFactory _factory;

        protected override void DoSetUp()
        {
            _factory = new TFactory();

            _context = RegisterForDisposal(new TestContext());
        }

        [Test]
        public void RaisesEventWheneverViewInstanceIsUpdated()
        {
            // arrange
            var viewManager = _factory.GetViewManager<View>();
            _context.AddViewManager(viewManager);

            var registeredUpdates = new Dictionary<Guid, int>();

            viewManager.Updated += view =>
            {
                if (!registeredUpdates.ContainsKey(view.AggregateRootId))
                {
                    registeredUpdates[view.AggregateRootId] = 0;
                }

                registeredUpdates[view.AggregateRootId]++;
            };

            // act
            var aggregateRootId1 = Guid.NewGuid();
            var aggregateRootId2 = Guid.NewGuid();
            _context.Save(aggregateRootId1, new Event());
            _context.Save(aggregateRootId1, new Event());
            _context.Save(aggregateRootId1, new Event());
            _context.Save(aggregateRootId2, new Event());

            // assert
            Assert.That(registeredUpdates.Count, Is.EqualTo(2));
            Assert.That(registeredUpdates[aggregateRootId1], Is.EqualTo(3));
            Assert.That(registeredUpdates[aggregateRootId2], Is.EqualTo(1));
        }

        public class Root : AggregateRoot { }
        public class Event : DomainEvent<Root> { }

        public class View : IViewInstance<InstancePerAggregateRootLocator>, ISubscribeTo<Event>
        {
            public string Id { get; set; }
            public long LastGlobalSequenceNumber { get; set; }
            public int EventCount { get; set; }
            public Guid AggregateRootId { get; set; }
            public void Handle(IViewContext context, Event domainEvent)
            {
                AggregateRootId = domainEvent.GetAggregateRootId();
                EventCount++;
            }
        }
    }
}
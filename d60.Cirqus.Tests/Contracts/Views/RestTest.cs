using System;
using d60.Cirqus.Aggregates;
using d60.Cirqus.Events;
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
    [Description("Verifies that the view managers can find 'rest', even when they don't get to have events dispatched to them (e.g. when the view does not subscribe to the events in question)")]
    public class RestTest<TFactory> : FixtureBase where TFactory : AbstractViewManagerFactory, new()
    {
        TestContext _context;
        TFactory _factory;

        protected override void DoSetUp()
        {
            _factory = new TFactory();

            _context = RegisterForDisposal(new TestContext());
        }

        [Test]
        public void ViewFindsRestEvenThoughItIsStillEmpty()
        {
            // arrange
            _context.AddViewManager(_factory.GetViewManager<View>());

            // act
            _context.Save(Guid.NewGuid(), new Event());

            // assert
        }

        /// <summary>
        /// View that does not subscribe to anything
        /// </summary>
        public class View : IViewInstance<InstancePerAggregateRootLocator>, ISubscribeTo
        {
            public string Id { get; set; }
            public long LastGlobalSequenceNumber { get; set; }
        }

        public class Root : AggregateRoot { }

        public class Event : DomainEvent<Root> { }
    }
}
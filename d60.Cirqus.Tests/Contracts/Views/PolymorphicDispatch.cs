using System;
using d60.Cirqus.Logging;
using d60.Cirqus.Logging.Console;
using d60.Cirqus.Tests.Contracts.Views.Factories;
using d60.Cirqus.Tests.Contracts.Views.Models.PolymorphicDispatch;
using d60.Cirqus.Views.ViewManagers;
using d60.Cirqus.Views.ViewManagers.Locators;
using NUnit.Framework;
using TestContext = d60.Cirqus.TestHelpers.TestContext;

namespace d60.Cirqus.Tests.Contracts.Views
{
    [TestFixture(typeof(MongoDbViewManagerFactory), Category = TestCategories.MongoDb)]
    [TestFixture(typeof(MsSqlViewManagerFactory), Category = TestCategories.MsSql)]
    [TestFixture(typeof(EntityFrameworkViewManagerFactory), Category = TestCategories.MsSql)]
    [TestFixture(typeof(InMemoryViewManagerFactory))]
    public class PolymorphicDispatch<TFactory> : FixtureBase where TFactory : AbstractViewManagerFactory, new()
    {
        TFactory _factory;
        TestContext _context;
        IViewManager<ViewThatSubscribesToEvents> _viewManager1;
        IViewManager<ViewThatSubscribesToAggregateRootEvent> _viewManager2;
        IViewManager<ViewThatSubscribesToAllEvents> _viewManager3;

        protected override void DoSetUp()
        {
            CirqusLoggerFactory.Current = new ConsoleLoggerFactory(minLevel: Logger.Level.Warn);

            _factory = RegisterForDisposal(new TFactory());

            _context = RegisterForDisposal(new TestContext { Asynchronous = true });

            _viewManager1 = _factory.GetViewManager<ViewThatSubscribesToEvents>();
            _viewManager2 = _factory.GetViewManager<ViewThatSubscribesToAggregateRootEvent>();
            _viewManager3 = _factory.GetViewManager<ViewThatSubscribesToAllEvents>();

            _context
                .AddViewManager(_viewManager1)
                .AddViewManager(_viewManager2)
                .AddViewManager(_viewManager3);
        }

        [Test]
        public void ViewsCanSubscribeToBaseClasses()
        {
            // arrange
            var aggregateRootId = Guid.NewGuid();
            var viewId = InstancePerAggregateRootLocator.GetViewIdFromAggregateRootId(aggregateRootId);

            _context.Save(aggregateRootId, new Event());
            _context.Save(aggregateRootId, new Event());
            _context.Save(aggregateRootId, new AnotherEvent());
            _context.Save(aggregateRootId, new AnotherEvent());

            _context.WaitForViewsToCatchUp();

            // act
            var normalView = _viewManager1.Load(viewId);
            var viewWithAggregateRootSubscription = _viewManager2.Load(viewId);
            var viewWithGeneralDomainEventSubscription = _viewManager3.Load(viewId);

            // assert
            Assert.That(normalView.ProcessedEvents, Is.EqualTo(4));
            Assert.That(viewWithAggregateRootSubscription.ProcessedEvents, Is.EqualTo(4), "Expected that the view could get all events from this particular aggregate root");
            Assert.That(viewWithGeneralDomainEventSubscription.ProcessedEvents, Is.EqualTo(4), "Expected that the view could get ALL events");
        }
    }
}
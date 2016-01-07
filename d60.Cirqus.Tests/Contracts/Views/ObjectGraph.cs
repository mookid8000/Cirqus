using d60.Cirqus.Logging;
using d60.Cirqus.Logging.Console;
using d60.Cirqus.Tests.Contracts.Views.Factories;
using d60.Cirqus.Tests.Contracts.Views.Models.ObjectGraph;
using d60.Cirqus.Views.ViewManagers.Locators;
using NUnit.Framework;
using TestContext = d60.Cirqus.Testing.TestContext;

namespace d60.Cirqus.Tests.Contracts.Views
{
    [TestFixture(typeof(MongoDbViewManagerFactory), Category = TestCategories.MongoDb)]
    [TestFixture(typeof(EntityFrameworkViewManagerFactory), Category = TestCategories.MsSql)]
    [TestFixture(typeof(InMemoryViewManagerFactory))]
    [Description("Some view managers must be able to save and load an entire object graph")]
    public class ObjectGraph<TFactory> : FixtureBase where TFactory : AbstractViewManagerFactory, new()
    {
        TFactory _factory;
        TestContext _context;

        protected override void DoSetUp()
        {
            CirqusLoggerFactory.Current = new ConsoleLoggerFactory(minLevel: Logger.Level.Warn);

            _factory = RegisterForDisposal(new TFactory());
            _context = RegisterForDisposal(TestContext.Create());

            _context.AddViewManager(_factory.GetViewManager<ViewRoot>());
        }

        [Test]
        public void WorksWithChildren()
        {
            var root1 = "B8FC0210-B7E4-4279-8BC8-53F13A557751";
            
            _context.Save(root1, new Event {NumberOfChildren = 3});

            var view = _factory.Load<ViewRoot>(InstancePerAggregateRootLocator.GetViewIdFromAggregateRootId(root1));
            
            Assert.That(view.Children.Count, Is.EqualTo(3));
        }

        [Test]
        public void ItWorksWithSeveralViewsAndDeletionOfRemovedChildren()
        {
            var root1 = "B8FC0210-B7E4-4279-8BC8-53F13A557751";
            var root2 = "394C50E4-245A-4C35-829D-B4625EDC59F4";

            _context.Save(root1, new Event { NumberOfChildren = 30 });
            _context.Save(root2, new Event { NumberOfChildren = 20 });
            
            _context.Save(root1, new Event {NumberOfChildren = 1});
            _context.Save(root2, new Event {NumberOfChildren = 2});

            var view1 = _factory.Load<ViewRoot>(InstancePerAggregateRootLocator.GetViewIdFromAggregateRootId(root1));
            var view2 = _factory.Load<ViewRoot>(InstancePerAggregateRootLocator.GetViewIdFromAggregateRootId(root2));
            
            Assert.That(view1.Children.Count, Is.EqualTo(1));
            Assert.That(view2.Children.Count, Is.EqualTo(2));
        }
    }
}
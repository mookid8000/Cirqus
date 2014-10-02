using System;
using d60.Cirqus.Logging;
using d60.Cirqus.Logging.Console;
using d60.Cirqus.Tests.Contracts.Views.Factories;
using d60.Cirqus.Tests.Contracts.Views.Models.LoadingStuffDuringViewLocation;
using NUnit.Framework;
using TestContext = d60.Cirqus.TestHelpers.TestContext;

namespace d60.Cirqus.Tests.Contracts.Views
{
    [TestFixture(typeof(MongoDbViewManagerFactory), Category = TestCategories.MongoDb)]
    [TestFixture(typeof(MsSqlViewManagerFactory), Category = TestCategories.MsSql)]
    [TestFixture(typeof(EntityFrameworkViewManagerFactory), Category = TestCategories.MsSql)]
    [TestFixture(typeof(InMemoryViewManagerFactory))]
    public class LoadingStuffDuringViewLocation<TFactory> : FixtureBase where TFactory : AbstractViewManagerFactory, new()
    {
        TFactory _factory;
        TestContext _context;

        protected override void DoSetUp()
        {
            CirqusLoggerFactory.Current = new ConsoleLoggerFactory(minLevel: Logger.Level.Debug);

            _factory = RegisterForDisposal(new TFactory());

            _context = RegisterForDisposal(new TestContext { Asynchronous = true });
        }

        [Test]
        public void CanLoadRootsDuringViewLocation()
        {
            _context.AddViewManager(_factory.GetViewManager<CountTheNodes>());

            // arrange
            var rootNodeId = Guid.NewGuid();
            using (var uow = _context.BeginUnitOfWork())
            {
                var node = uow.Get<Node>(rootNodeId);

                var child1 = uow.Get<Node>(Guid.NewGuid());
                var child2 = uow.Get<Node>(Guid.NewGuid());

                child1.AttachTo(node);
                child2.AttachTo(node);

                var subChild1 = uow.Get<Node>(Guid.NewGuid());
                var subChild2 = uow.Get<Node>(Guid.NewGuid());
                var subChild3 = uow.Get<Node>(Guid.NewGuid());

                subChild1.AttachTo(child1);
                subChild2.AttachTo(child1);
                subChild3.AttachTo(child2);

                // act
                uow.Commit();
            }

            _context.WaitForViewToCatchUp<CountTheNodes>();

            // assert
            var view = _factory.Load<CountTheNodes>(rootNodeId.ToString());
            Assert.That(view.Nodes, Is.EqualTo(5));
        }
    }
}
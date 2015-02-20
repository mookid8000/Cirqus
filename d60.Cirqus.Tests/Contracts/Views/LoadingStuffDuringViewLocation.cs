using d60.Cirqus.Logging;
using d60.Cirqus.Logging.Console;
using d60.Cirqus.Testing;
using d60.Cirqus.Tests.Contracts.Views.Factories;
using d60.Cirqus.Tests.Contracts.Views.Models.LoadingStuffDuringViewLocation;
using NUnit.Framework;
using TestContext = d60.Cirqus.Testing.TestContext;

namespace d60.Cirqus.Tests.Contracts.Views
{
    [TestFixture(typeof(MongoDbViewManagerFactory), Category = TestCategories.MongoDb)]
    [TestFixture(typeof(PostgreSqlViewManagerFactory), Category = TestCategories.PostgreSql)]
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

            _context = RegisterForDisposal(
                TestContext.With()
                    .Options(x => x.Asynchronous())
                    .Create());
        }

        [Test]
        public void CanLoadRootsDuringViewLocation()
        {
            _context.AddViewManager(_factory.GetViewManager<CountTheNodes>());

            // arrange
            using (var uow = _context.BeginUnitOfWork())
            {
                var node = uow.Load<Node>("rootnodeid");

                var child1 = uow.Load<Node>("child1");
                var child2 = uow.Load<Node>("child2");

                child1.AttachTo(node);
                child2.AttachTo(node);

                var subChild1 = uow.Load<Node>("subchild1");
                var subChild2 = uow.Load<Node>("subchild2");
                var subChild3 = uow.Load<Node>("subchild3");

                subChild1.AttachTo(child1);
                subChild2.AttachTo(child1);
                subChild3.AttachTo(child2);

                // act
                uow.Commit();
            }

            _context.WaitForViewToCatchUp<CountTheNodes>();

            // assert
            var view = _factory.Load<CountTheNodes>("rootnodeid");
            Assert.That(view.Nodes, Is.EqualTo(5));
        }
    }
}
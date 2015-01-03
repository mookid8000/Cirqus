using d60.Cirqus.Tests.Contracts.Views.Factories;
using d60.Cirqus.Tests.Contracts.Views.Models.ViewLocators;
using d60.Cirqus.Views.ViewManagers;
using d60.Cirqus.Views.ViewManagers.Locators;
using NUnit.Framework;
using TestContext = d60.Cirqus.Testing.TestContext;

namespace d60.Cirqus.Tests.Contracts.Views
{
    [TestFixture(typeof(MongoDbViewManagerFactory), Category = TestCategories.MongoDb)]
    [TestFixture(typeof(PostgreSqlViewManagerFactory), Category = TestCategories.PostgreSql)]
    [TestFixture(typeof(MsSqlViewManagerFactory), Category = TestCategories.MsSql)]
    [TestFixture(typeof(EntityFrameworkViewManagerFactory), Category = TestCategories.MsSql)]
    [TestFixture(typeof(InMemoryViewManagerFactory))]
    public class ViewLocators<TFactory> : FixtureBase where TFactory : AbstractViewManagerFactory, new()
    {
        TFactory _factory;
        TestContext _context;

        IViewManager<InstancePerAggregateRootView> _instancePerAggregateRootViewManager;
        IViewManager<GlobalInstanceViewInstance> _globalInstanceViewManager;

        protected override void DoSetUp()
        {
            _factory = RegisterForDisposal(new TFactory());

            _globalInstanceViewManager = _factory.GetViewManager<GlobalInstanceViewInstance>();
            _instancePerAggregateRootViewManager = _factory.GetViewManager<InstancePerAggregateRootView>();

            _context = RegisterForDisposal(new TestContext { Asynchronous = true });
        }

        [Test]
        public void WorksWithInstancePerAggregateRootView()
        {
            _context.AddViewManager(_instancePerAggregateRootViewManager);

            _context.Save("id1", new ThisIsJustAnEvent());
            _context.Save("id1", new ThisIsJustAnEvent());
            _context.Save("id1", new ThisIsJustAnEvent());

            _context.Save("id2", new ThisIsJustAnEvent());
            _context.Save("id2", new ThisIsJustAnEvent());
            _context.Save("id2", new ThisIsJustAnEvent());

            _context.WaitForViewsToCatchUp();

            var view = _factory.Load<InstancePerAggregateRootView>(InstancePerAggregateRootLocator.GetViewIdFromAggregateRootId("id1"));
            Assert.That(view.EventCounter, Is.EqualTo(3));
        }

        [Test]
        public void WorksWithGlobalInstanceView()
        {
            _context.AddViewManager(_globalInstanceViewManager);

            _context.Save("id1", new ThisIsJustAnEvent());
            _context.Save("id1", new ThisIsJustAnEvent());
            _context.Save("id1", new ThisIsJustAnEvent());

            _context.Save("id2", new ThisIsJustAnEvent());
            _context.Save("id2", new ThisIsJustAnEvent());
            _context.Save("id2", new ThisIsJustAnEvent());

            _context.WaitForViewsToCatchUp();

            var view = _factory.Load<GlobalInstanceViewInstance>(GlobalInstanceLocator.GetViewInstanceId());
            Assert.That(view.EventCounter, Is.EqualTo(6));
        }

        [Test]
        public void DoesNotCallViewLocatorForIrrelevantEvents()
        {
            _context.AddViewManager(_globalInstanceViewManager);

            Assert.DoesNotThrow(() => _context.Save("id1", new JustAnEvent()));
            Assert.DoesNotThrow(() => _context.Save("id2", new AnotherEvent()));
        }
    }
}
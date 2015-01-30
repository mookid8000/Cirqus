using d60.Cirqus.Tests.Contracts.Views.Factories;
using d60.Cirqus.Views.ViewManagers;
using d60.Cirqus.Views.ViewManagers.Locators;
using NUnit.Framework;
using TestContext = d60.Cirqus.Testing.TestContext;
using d60.Cirqus.Tests.Contracts.Views.Models.ViewInstanceDeletion;

namespace d60.Cirqus.Tests.Contracts.Views
{
    [TestFixture(typeof(MongoDbViewManagerFactory), Category = TestCategories.MongoDb)]
    [TestFixture(typeof(PostgreSqlViewManagerFactory), Category = TestCategories.PostgreSql)]
    [TestFixture(typeof(MsSqlViewManagerFactory), Category = TestCategories.MsSql)]
    [TestFixture(typeof(EntityFrameworkViewManagerFactory), Category = TestCategories.MsSql)]
    [TestFixture(typeof(InMemoryViewManagerFactory))]
    [Description("Verifies that view managers are capable of deleting a view instance")]
    public class ViewInstanceDeletion<TFactory> : FixtureBase where TFactory : AbstractViewManagerFactory, new()
    {
        TFactory _factory;
        TestContext _context;

        protected override void DoSetUp()
        {
            _factory = RegisterForDisposal(new TFactory());

            _context = RegisterForDisposal(TestContext.Create());
        }

        [Test, Ignore("hmmmm...")]
        public void YayItWorks()
        {
            var viewManager = _factory.GetViewManager<ViewThatCanBeDeleted>();

            _context.ProcessCommand(new MakeStuffHappen("bim"));
            _context.ProcessCommand(new MakeStuffHappen("bim"));

            _context.ProcessCommand(new MakeStuffHappen("bom"));
            _context.ProcessCommand(new MakeStuffHappen("bom"));

            _context.ProcessCommand(new Undo("bim"));

            var bimView = viewManager.Load("bim");
            var bomView = viewManager.Load("bom");

            Assert.That(bomView, Is.Not.Null);
            Assert.That(bomView.ThisManyThingsHappened, Is.EqualTo(2));

            Assert.That(bimView, Is.Null, "Did not expect an instance of the 'bim' view to be present because it's deleted");
        }

        public class ViewThatCanBeDeleted : IViewInstance<InstancePerAggregateRootLocator>, ISubscribeTo<SomethingHappened>, ISubscribeTo<Undone>
        {
            public string Id { get; set; }
            public long LastGlobalSequenceNumber { get; set; }
            public int ThisManyThingsHappened { get; set; }
            
            public void Handle(IViewContext context, SomethingHappened domainEvent)
            {
                ThisManyThingsHappened++;
            }

            public void Handle(IViewContext context, Undone domainEvent)
            {
                //context.DeleteThisViewInstance();
            }
        }
    }
}
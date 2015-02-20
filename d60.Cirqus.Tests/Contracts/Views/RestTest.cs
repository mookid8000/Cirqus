using d60.Cirqus.Tests.Contracts.Views.Factories;
using d60.Cirqus.Tests.Contracts.Views.Models.RestTest;
using NUnit.Framework;
using TestContext = d60.Cirqus.Testing.TestContext;

namespace d60.Cirqus.Tests.Contracts.Views
{
    [TestFixture(typeof(MongoDbViewManagerFactory), Category = TestCategories.MongoDb)]
    [TestFixture(typeof(PostgreSqlViewManagerFactory), Category = TestCategories.PostgreSql)]
    [TestFixture(typeof(MsSqlViewManagerFactory), Category = TestCategories.MsSql)]
    [TestFixture(typeof(EntityFrameworkViewManagerFactory), Category = TestCategories.MsSql)]
    [TestFixture(typeof(InMemoryViewManagerFactory))]
    [Description("Verifies that the view managers can find 'rest', even when they don't get to have events dispatched to them (e.g. when the view does not subscribe to the events in question)")]
    public class RestTest<TFactory> : FixtureBase where TFactory : AbstractViewManagerFactory, new()
    {
        TestContext _context;
        TFactory _factory;

        protected override void DoSetUp()
        {
            _factory = RegisterForDisposal(new TFactory());

            _context = RegisterForDisposal(TestContext.Create());
        }

        [Test]
        public void ViewFindsRestEvenThoughItIsStillEmpty()
        {
            // arrange
            _context.AddViewManager(_factory.GetViewManager<View>());

            // act
            _context.Save("key", new Event());

            // assert
        }
    }
}
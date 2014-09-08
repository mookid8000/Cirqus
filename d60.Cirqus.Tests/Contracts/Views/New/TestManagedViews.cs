using System.Threading;
using d60.Cirqus.Tests.Contracts.Views.New.Factories;
using d60.Cirqus.Tests.Contracts.Views.New.Models;
using d60.Cirqus.Views.ViewManagers.Locators;
using NUnit.Framework;
using TestContext = d60.Cirqus.TestHelpers.TestContext;

namespace d60.Cirqus.Tests.Contracts.Views.New
{
    [TestFixture(typeof(MongoDbManagedViewFactory), Category = TestCategories.MongoDb)]
    public class TestManagedViews<TFactory> : FixtureBase where TFactory : IManagedViewFactory, new()
    {
        TFactory _factory;
        TestContext _context;

        protected override void DoSetUp()
        {
            _factory = new TFactory();

            _context = new TestContext();
        }

        [Test]
        public void WorksWithSimpleScenario()
        {
            // arrange
            var view = _factory.CreateManagedView<GeneratedIds>();
            _context.AddViewManager(view);

            // act
            _context.ProcessCommand(new GenerateNewId(IdGenerator.InstanceId) { IdBase = "bim" });
            _context.ProcessCommand(new GenerateNewId(IdGenerator.InstanceId) { IdBase = "bim" });
            var last = _context.ProcessCommand(new GenerateNewId(IdGenerator.InstanceId) { IdBase = "bom" });

            // assert
            view.WaitUntilDispatched(last).Wait();

            var idsView = view.Load(InstancePerAggregateRootLocator.GetViewIdFromAggregateRootId(IdGenerator.InstanceId));
            
            Assert.That(idsView.AllIds.Count, Is.EqualTo(3));
         
            Assert.That(idsView.AllIds, Contains.Item("bim/0"));
            Assert.That(idsView.AllIds, Contains.Item("bim/1"));
            Assert.That(idsView.AllIds, Contains.Item("bom/0"));
        }
    }
}
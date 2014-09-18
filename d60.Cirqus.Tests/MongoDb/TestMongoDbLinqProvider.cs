using System;
using System.Linq;
using d60.Cirqus.Aggregates;
using d60.Cirqus.Events;
using d60.Cirqus.MongoDb.Projections.Views.Old;
using d60.Cirqus.Projections.Views.ViewManagers;
using d60.Cirqus.Projections.Views.ViewManagers.Locators;
using NUnit.Framework;
using TestContext = d60.Cirqus.TestHelpers.TestContext;

namespace d60.Cirqus.Tests.MongoDb
{
    [TestFixture]
    public class TestMongoDbLinqProvider : FixtureBase
    {
        TestContext _context;
        MongoDbViewManager<RootViewInstance> _viewManager;

        protected override void DoSetUp()
        {
            _viewManager = new MongoDbViewManager<RootViewInstance>(MongoHelper.InitializeTestDatabase(), "rootViews");

            _context = RegisterForDisposal(new TestContext())
                .AddViewManager(_viewManager);

            _viewManager.CreateIndex(v => v.Name);
        }

        [Test]
        public void CanGenerateViewAndLoadByAnything()
        {
            var root1 = Guid.NewGuid();
            var root2 = Guid.NewGuid();
            var root3 = Guid.NewGuid();

            _context.Save(root1, new RootWasNamed { Name = "Frank" });
            _context.Save(root1, new RootWasNamed { Name = "Francis" });
            _context.Save(root2, new RootWasNamed { Name = "Claire" });
            _context.Save(root3, new RootWasNamed { Name = "Doug" });

            var view1 = _viewManager.Load(InstancePerAggregateRootLocator.GetViewIdFromAggregateRootId(root1));
            var view2 = _viewManager.Load(InstancePerAggregateRootLocator.GetViewIdFromAggregateRootId(root2));
            var view3 = _viewManager.Load(InstancePerAggregateRootLocator.GetViewIdFromAggregateRootId(root3));

            Assert.That(view1.Name, Is.EqualTo("Francis"));
            Assert.That(view2.Name, Is.EqualTo("Claire"));
            Assert.That(view3.Name, Is.EqualTo("Doug"));

            Console.WriteLine(string.Join(Environment.NewLine, _viewManager.Linq().ToList().Select(v => v.Name)));

            var francis = _viewManager.Linq().First(v => v.Name == "Francis");
            
            Assert.That(francis.Name, Is.EqualTo("Francis"));
        }
    }

    class Root : AggregateRoot { }
    class RootWasNamed : DomainEvent<Root> { public string Name { get; set; } }
    class RootViewInstance : IViewInstance<InstancePerAggregateRootLocator>,
        ISubscribeTo<RootWasNamed>
    {
        public string Id { get; set; }
        public long LastGlobalSequenceNumber { get; set; }
        public string Name { get; set; }
        public void Handle(IViewContext context, RootWasNamed domainEvent)
        {
            Name = domainEvent.Name;
        }
    }
}
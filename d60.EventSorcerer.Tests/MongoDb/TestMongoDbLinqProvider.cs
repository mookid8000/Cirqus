using System;
using System.Linq;
using d60.EventSorcerer.Aggregates;
using d60.EventSorcerer.Events;
using d60.EventSorcerer.MongoDb.Views;
using d60.EventSorcerer.Tests.Contracts.Views;
using d60.EventSorcerer.Views.Basic;
using d60.EventSorcerer.Views.Basic.Locators;
using NUnit.Framework;
using TestContext = d60.EventSorcerer.TestHelpers.TestContext;

namespace d60.EventSorcerer.Tests.MongoDb
{
    [TestFixture]
    public class TestMongoDbLinqProvider : FixtureBase
    {
        TestContext _context;
        MongoDbCatchUpViewManager<RootView> _viewManager;

        protected override void DoSetUp()
        {
            _viewManager = new MongoDbCatchUpViewManager<RootView>(MongoHelper.InitializeTestDatabase(), "rootViews");

            _context = new TestContext()
                .AddViewManager(_viewManager);
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
            _context.Commit();

            var view1 = _viewManager.Load(InstancePerAggregateRootLocator.GetViewIdFromGuid(root1));
            var view2 = _viewManager.Load(InstancePerAggregateRootLocator.GetViewIdFromGuid(root2));
            var view3 = _viewManager.Load(InstancePerAggregateRootLocator.GetViewIdFromGuid(root3));

            Assert.That(view1.Name, Is.EqualTo("Francis"));
            Assert.That(view2.Name, Is.EqualTo("Claire"));
            Assert.That(view3.Name, Is.EqualTo("Doug"));

            Console.WriteLine(string.Join(Environment.NewLine, _viewManager.Linq().ToList().Select(v => v.View.Name)));

            var francis = _viewManager.Linq().FirstOrDefault(v => v.View.Name == "Francis");
            Assert.That(francis, Is.Not.Null, "Expected to find view with Name == 'Francis'");
            Assert.That(francis.View.Name, Is.EqualTo("Francis"));
        }
    }

    class Root : AggregateRoot { }
    class RootWasNamed : DomainEvent<Root> { public string Name { get; set; } }
    class RootView : IView<InstancePerAggregateRootLocator>,
        ISubscribeTo<RootWasNamed>
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public void Handle(IViewContext context, RootWasNamed domainEvent)
        {
            Name = domainEvent.Name;
        }
    }
}
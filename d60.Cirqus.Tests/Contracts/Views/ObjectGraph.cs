using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using d60.Cirqus.Aggregates;
using d60.Cirqus.Events;
using d60.Cirqus.Logging;
using d60.Cirqus.Logging.Console;
using d60.Cirqus.Tests.Contracts.Views.Factories;
using d60.Cirqus.Views;
using d60.Cirqus.Views.ViewManagers;
using d60.Cirqus.Views.ViewManagers.Locators;
using NUnit.Framework;
using TestContext = d60.Cirqus.TestHelpers.TestContext;

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
        IViewManager<ViewRoot> _viewManager;

        protected override void DoSetUp()
        {
            CirqusLoggerFactory.Current = new ConsoleLoggerFactory(minLevel: Logger.Level.Warn);

            _factory = new TFactory();

            _context = RegisterForDisposal(new TestContext());

            _viewManager = _factory.GetViewManager<ViewRoot>();
            _context.AddViewManager(_viewManager);
        }

        [Test]
        public void WorksWithChildren()
        {
            var root1 = new Guid("B8FC0210-B7E4-4279-8BC8-53F13A557751");
            
            _context.Save(root1, new Event {NumberOfChildren = 3});

            var view = _viewManager.Load(InstancePerAggregateRootLocator.GetViewIdFromAggregateRootId(root1));
            Assert.That(view.Children.Count, Is.EqualTo(3));
        }

        [Test]
        public void ItWorksWithSevealViewsAndDeletionOfRemovedChildren()
        {
            var root1 = new Guid("B8FC0210-B7E4-4279-8BC8-53F13A557751");
            var root2 = new Guid("394C50E4-245A-4C35-829D-B4625EDC59F4");
            _context.Save(root1, new Event { NumberOfChildren = 30 });
            _context.Save(root2, new Event { NumberOfChildren = 20 });
            
            _context.Save(root1, new Event {NumberOfChildren = 1});
            _context.Save(root2, new Event {NumberOfChildren = 2});

            var view1 = _viewManager.Load(InstancePerAggregateRootLocator.GetViewIdFromAggregateRootId(root1));
            var view2 = _viewManager.Load(InstancePerAggregateRootLocator.GetViewIdFromAggregateRootId(root2));
            Assert.That(view1.Children.Count, Is.EqualTo(1));
            Assert.That(view2.Children.Count, Is.EqualTo(2));
        }


        public class Root : AggregateRoot { }

        public class Event : DomainEvent<Root>
        {
            public int NumberOfChildren { get; set; }
        }

        public class ViewRoot : IViewInstance<InstancePerAggregateRootLocator>, ISubscribeTo<Event>
        {
            public ViewRoot()
            {
                Children = new List<ViewChild>();
            }

            public string Id { get; set; }

            public long LastGlobalSequenceNumber { get; set; }

            public List<ViewChild> Children { get; set; }

            public void Handle(IViewContext context, Event domainEvent)
            {
                while (Children.Count < domainEvent.NumberOfChildren) AddChild();
                while (Children.Count > domainEvent.NumberOfChildren) RemoveChild();
            }

            void AddChild()
            {
                Children.Add(new ViewChild { Something = "klokken er " + DateTime.Now });
            }

            void RemoveChild()
            {
                Children.Remove(Children.Last());
            }
        }

        public class ViewChild
        {
            public string Something { get; set; }
        }
    }
}
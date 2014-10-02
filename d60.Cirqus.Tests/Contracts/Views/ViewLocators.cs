using System;
using System.Collections.Generic;
using d60.Cirqus.Aggregates;
using d60.Cirqus.Events;
using d60.Cirqus.Tests.Contracts.Views.Factories;
using d60.Cirqus.Views.ViewManagers;
using d60.Cirqus.Views.ViewManagers.Locators;
using NUnit.Framework;
using TestContext = d60.Cirqus.TestHelpers.TestContext;

namespace d60.Cirqus.Tests.Contracts.Views
{
    [TestFixture(typeof(MongoDbViewManagerFactory), Category = TestCategories.MongoDb)]
    [TestFixture(typeof(MsSqlViewManagerFactory), Category = TestCategories.MsSql)]
    [TestFixture(typeof(EntityFrameworkViewManagerFactory), Category = TestCategories.MsSql)]
    [TestFixture(typeof(InMemoryViewManagerFactory))]
    public class ViewLocators<TViewManagerFactory> : FixtureBase where TViewManagerFactory : AbstractViewManagerFactory, new()
    {
        TViewManagerFactory _factory;
        TestContext _context;

        IViewManager<InstancePerAggregateRootView> _instancePerAggregateRootViewManager;
        IViewManager<GlobalInstanceViewInstance> _globalInstanceViewManager;

        protected override void DoSetUp()
        {
            _factory = new TViewManagerFactory();

            _globalInstanceViewManager = _factory.GetViewManager<GlobalInstanceViewInstance>();
            _instancePerAggregateRootViewManager = _factory.GetViewManager<InstancePerAggregateRootView>();

            _context = RegisterForDisposal(new TestContext { Asynchronous = true });
        }


        [Test]
        public void WorksWithInstancePerAggregateRootView()
        {
            _context.AddViewManager(_instancePerAggregateRootViewManager);

            var rootId1 = Guid.NewGuid();
            var rootId2 = Guid.NewGuid();

            _context.Save(rootId1, new ThisIsJustAnEvent());
            _context.Save(rootId1, new ThisIsJustAnEvent());
            _context.Save(rootId1, new ThisIsJustAnEvent());

            _context.Save(rootId2, new ThisIsJustAnEvent());
            _context.Save(rootId2, new ThisIsJustAnEvent());
            _context.Save(rootId2, new ThisIsJustAnEvent());

            _context.WaitForViewsToCatchUp();

            var view = _factory.Load<InstancePerAggregateRootView>(InstancePerAggregateRootLocator.GetViewIdFromAggregateRootId(rootId1));
            Assert.That(view.EventCounter, Is.EqualTo(3));
        }

        [Test]
        public void WorksWithGlobalInstanceView()
        {
            _context.AddViewManager(_globalInstanceViewManager);

            var rootId1 = Guid.NewGuid();
            var rootId2 = Guid.NewGuid();

            _context.Save(rootId1, new ThisIsJustAnEvent());
            _context.Save(rootId1, new ThisIsJustAnEvent());
            _context.Save(rootId1, new ThisIsJustAnEvent());

            _context.Save(rootId2, new ThisIsJustAnEvent());
            _context.Save(rootId2, new ThisIsJustAnEvent());
            _context.Save(rootId2, new ThisIsJustAnEvent());

            _context.WaitForViewsToCatchUp();

            var view = _factory.Load<GlobalInstanceViewInstance>(GlobalInstanceLocator.GetViewInstanceId());
            Assert.That(view.EventCounter, Is.EqualTo(6));
        }

        [Test]
        public void DoesNotCallViewLocatorForIrrelevantEvents()
        {
            _context.AddViewManager(_globalInstanceViewManager);

            Assert.DoesNotThrow(() => _context.Save(Guid.NewGuid(), new JustAnEvent()));

            Assert.DoesNotThrow(() => _context.Save(Guid.NewGuid(), new AnotherEvent()));
        }

        class MyViewInstance : IViewInstance<CustomizedViewLocator>, ISubscribeTo<JustAnEvent>
        {
            public string Id { get; set; }
            public long LastGlobalSequenceNumber { get; set; }

            public void Handle(IViewContext context, JustAnEvent domainEvent)
            {

            }
        }

        class CustomizedViewLocator : ViewLocator
        {
            protected override IEnumerable<string> GetViewIds(IViewContext context, DomainEvent e)
            {
                if (e is JustAnEvent)
                {
                    yield return "yay";
                }
                else
                {
                    throw new ApplicationException("oh noes!!!!");
                }
            }
        }
    }

    class JustAnEvent : DomainEvent<Root>
    {
    }
    class AnotherEvent : DomainEvent<Root>
    {
    }
    class Root : AggregateRoot
    {
    }
    class GlobalInstanceViewInstance : IViewInstance<GlobalInstanceLocator>, ISubscribeTo<ThisIsJustAnEvent>
    {
        public int EventCounter { get; set; }
        public void Handle(IViewContext context, ThisIsJustAnEvent domainEvent)
        {
            EventCounter++;
        }

        public string Id { get; set; }
        public long LastGlobalSequenceNumber { get; set; }
    }
    class InstancePerAggregateRootView : IViewInstance<InstancePerAggregateRootLocator>, ISubscribeTo<ThisIsJustAnEvent>
    {
        public int EventCounter { get; set; }
        public void Handle(IViewContext context, ThisIsJustAnEvent domainEvent)
        {
            EventCounter++;
        }

        public string Id { get; set; }
        public long LastGlobalSequenceNumber { get; set; }
    }

    class ThisIsJustAnEvent : DomainEvent<Root>
    {
    }
}
using System;
using System.Linq;
using System.Threading.Tasks;
using d60.Cirqus.Extensions;
using d60.Cirqus.Tests.Extensions;
using d60.Cirqus.Tests.Views.TestAggregateRootView.Model;
using d60.Cirqus.Views.ViewManagers;
using d60.Cirqus.Views.ViewManagers.Locators;
using NUnit.Framework;

namespace d60.Cirqus.Tests.Views.TestAggregateRootView
{
    [TestFixture]
    public class AggregateRootViewWorks : FixtureBase
    {
        ICommandProcessor _commandProcessor;
        InMemoryViewManager<AggregateRootView> _viewManager;
        ViewManagerWaitHandle _waitHandler;

        protected override void DoSetUp()
        {
            _viewManager = new InMemoryViewManager<AggregateRootView>();

            _waitHandler = new ViewManagerWaitHandle();

            _commandProcessor = CommandProcessor.With()
                .EventStore(e => e.UseInMemoryEventStore())
                .EventDispatcher(e => e.UseViewManagerEventDispatcher(_viewManager).WithWaitHandle(_waitHandler))
                .Create();

            RegisterForDisposal(_commandProcessor);
        }

        [Test]
        public async Task YeahItWorks()
        {
            var lastResult = Enumerable.Range(0,40)
                .Select(i => _commandProcessor.ProcessCommand(new IncrementNumberCommand("id1")))
                .Last();

            await _waitHandler.WaitForAll(lastResult, TimeSpan.FromSeconds(10));

            var view = _viewManager.Load("id1");

            Assert.That(view.Counter, Is.EqualTo(40));
        }

        class AggregateRootView : IViewInstance<InstancePerAggregateRootLocator>, ISubscribeTo<NumberEvent>
        {
            public string Id { get; set; }
            public long LastGlobalSequenceNumber { get; set; }

            public int Counter { get; set; }

            public void Handle(IViewContext context, NumberEvent domainEvent)
            {
                var aggregateRootId = domainEvent.GetAggregateRootId();
                var instance = context.Load<AggregateRootWithLogic>(aggregateRootId);
                Counter = instance.Counter;
            }
        }
    }
}
using System;
using System.Collections;
using System.Collections.Generic;
using d60.Cirqus.Aggregates;
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

        protected override void DoSetUp()
        {
            _viewManager = new InMemoryViewManager<AggregateRootView>();

            _commandProcessor = CommandProcessor.With()
                .EventStore(e => e.UseInMemoryEventStore())
                .EventDispatcher(e => e.UseViewManagerEventDispatcher(_viewManager))
                .Create();

            RegisterForDisposal(_commandProcessor);
        }

        [Test]
        public void YeahItWorks()
        {
            _commandProcessor.ProcessCommand(new IncrementNumberCommand("id1"));
            _commandProcessor.ProcessCommand(new IncrementNumberCommand("id1"));
            _commandProcessor.ProcessCommand(new IncrementNumberCommand("id1"));
            _commandProcessor.ProcessCommand(new IncrementNumberCommand("id1"));

        }

        class AggregateRootView : IViewInstance<InstancePerAggregateRootLocator>, ISubscribeTo<NumberEvent>, IAggregateRootView
        {
            public string Id { get; set; }
            public long LastGlobalSequenceNumber { get; set; }

            public string AggregateRootId { get; set; }
            public int Counter { get; set; }

            public void Handle(IViewContext context, NumberEvent domainEvent)
            {
                AggregateRootId = domainEvent.GetAggregateRootId();

                AggregateRoots.Follow(AggregateRootId);

                var instance = context.Load<AggregateRootWithLogic>(AggregateRootId);

                Counter = instance.Counter;
            }

            public AggregateRootToFollow AggregateRoots { get; set; }
        }
    }
}
using System;
using System.Collections.Generic;
using d60.Cirqus.Aggregates;
using d60.Cirqus.Commands;
using d60.Cirqus.Config;
using d60.Cirqus.Events;
using d60.Cirqus.Extensions;
using d60.Cirqus.Tests.Extensions;
using d60.Cirqus.Views.ViewManagers;
using d60.Cirqus.Views.ViewManagers.Locators;
using NUnit.Framework;

namespace d60.Cirqus.Tests.Aggregates
{
    [TestFixture]
    public class TestAggregateRootLoadingWithInterfaces : FixtureBase
    {
        ICommandProcessor _commandProcessor;
        InMemoryViewManager<RootView> _viewManager;

        protected override void DoSetUp()
        {
            _viewManager = new InMemoryViewManager<RootView>();
            
            _commandProcessor = CommandProcessor.With()
                .EventStore(e => e.InMemoryEventStore())
                .EventDispatcher(e => e.UseViewManagerEventDispatcher(_viewManager))
                .Create();

            RegisterForDisposal(_commandProcessor);
        }

        [Test]
        public void CanLoadAggregateRootAsInterfaceWhenItExistsAlready()
        {
            // first, make sure an aggregate root exists
            const string aggregateRootId = "someId";
            _commandProcessor.ProcessCommand(new CustomCommand(context =>
            {
                var instance = context.Load<Root>(aggregateRootId);

                instance.DoStuff();
            }));

            // act
            var result = _commandProcessor.ProcessCommand(new CustomCommand(context =>
            {
                var instance = context.Load<ICanDoStuff>(aggregateRootId);

                instance.DoStuff();
            }));
            _viewManager.WaitUntilProcessed(result, TimeSpan.FromSeconds(5)).Wait();

            // assert
            var view = _viewManager.Load(GlobalInstanceLocator.GetViewInstanceId());
            Assert.That(view.EventCounts[aggregateRootId], Is.EqualTo(2));
        }

        class RootView : IViewInstance<GlobalInstanceLocator>, ISubscribeTo<RootEvent>
        {
            public RootView()
            {
                EventCounts = new Dictionary<string, int>();
            }
            public string Id { get; set; }
            public long LastGlobalSequenceNumber { get; set; }
            public Dictionary<string, int> EventCounts { get; set; }
            public void Handle(IViewContext context, RootEvent domainEvent)
            {
                var aggregateRootId = domainEvent.GetAggregateRootId();
                
                EventCounts[aggregateRootId] = EventCounts.ContainsKey(aggregateRootId)
                    ? EventCounts[aggregateRootId] + 1
                    : 1;
            }
        }

        interface ICanDoStuff
        {
            void DoStuff();
        }

        class Root : AggregateRoot, IEmit<RootEvent>, ICanDoStuff
        {
            public void DoStuff()
            {
                Emit(new RootEvent());
            }

            public void Apply(RootEvent e)
            {
            }
        }

        class RootEvent : DomainEvent<Root> { }

        class CustomCommand : ExecutableCommand
        {
            readonly Action<ICommandContext> _whatToDo;

            public CustomCommand(Action<ICommandContext> whatToDo)
            {
                _whatToDo = whatToDo;
            }

            public override void Execute(ICommandContext context)
            {
                _whatToDo(context);
            }
        }
    }
}
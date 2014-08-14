using System;
using System.Collections.Generic;
using System.Linq;
using d60.Cirqus.Aggregates;
using d60.Cirqus.Commands;
using d60.Cirqus.Config;
using d60.Cirqus.Events;
using d60.Cirqus.Extensions;
using d60.Cirqus.TestHelpers.Internals;
using d60.Cirqus.Views.ViewManagers;
using d60.Cirqus.Views.ViewManagers.Locators;
using NUnit.Framework;

namespace d60.Cirqus.Tests.Views
{
    [TestFixture]
    public class TestLoadAggregatesFromView : FixtureBase
    {
        CommandProcessor _cirqus;
        InMemoryViewManager<MyViewInstance> _viewManager1;
        InMemoryViewManager<MyViewInstanceImplicit> _viewManager2;

        protected override void DoSetUp()
        {
            var eventStore = new InMemoryEventStore();
            _viewManager1 = new InMemoryViewManager<MyViewInstance>();
            _viewManager2 = new InMemoryViewManager<MyViewInstanceImplicit>();

            var basicAggregateRootRepository = new DefaultAggregateRootRepository(eventStore);

            _cirqus = new CommandProcessor(eventStore, basicAggregateRootRepository, new ViewManagerEventDispatcher(basicAggregateRootRepository, _viewManager1, _viewManager2));

            _cirqus.Initialize();
        }

        [Test]
        public void CanAccessAggregateRootInView()
        {
            var aggregateRootId = Guid.NewGuid();

            _cirqus.ProcessCommand(new MyCommand(aggregateRootId));
            _cirqus.ProcessCommand(new MyCommand(aggregateRootId));
            _cirqus.ProcessCommand(new MyCommand(aggregateRootId));
            _cirqus.ProcessCommand(new MyCommand(aggregateRootId));
            _cirqus.ProcessCommand(new MyCommand(aggregateRootId));

            var view = _viewManager1.Load(InstancePerAggregateRootLocator.GetViewIdFromGuid(aggregateRootId));
            
            Assert.That(view.Calls.All(c => c.Item1 == c.Item2), "Registered calls contained a call where the version of the loaded aggregate root did not correspond to the version of the event that the view got to process: {0}",
                string.Join(", ", view.Calls.Select(c => string.Format("{0}/{1}", c.Item1, c.Item2))));
        }

        [Test]
        public void CanAccessAggregateRootInViewWithImplicitDeductionOfGlobalSequenceNumberInView()
        {
            var aggregateRootId = Guid.NewGuid();

            _cirqus.ProcessCommand(new MyCommand(aggregateRootId));
            _cirqus.ProcessCommand(new MyCommand(aggregateRootId));
            _cirqus.ProcessCommand(new MyCommand(aggregateRootId));
            _cirqus.ProcessCommand(new MyCommand(aggregateRootId));
            _cirqus.ProcessCommand(new MyCommand(aggregateRootId));

            var view = _viewManager2.Load(InstancePerAggregateRootLocator.GetViewIdFromGuid(aggregateRootId));
            
            Assert.That(view, Is.Not.Null);

            Assert.That(view.Calls.All(c => c.Item1 == c.Item2), "Registered calls contained a call where the version of the loaded aggregate root did not correspond to the version of the event that the view got to process: {0}",
                string.Join(", ", view.Calls.Select(c => string.Format("{0}/{1}", c.Item1, c.Item2))));
        }

        class MyCommand : Command<MyRoot>
        {
            public MyCommand(Guid aggregateRootId) : base(aggregateRootId)
            {
            }

            public override void Execute(MyRoot aggregateRoot)
            {
                aggregateRoot.DoStuff();
            }
        }

        public class MyRoot : AggregateRoot, IEmit<AnEvent> 
        {
            public void DoStuff()
            {
                Emit(new AnEvent {EventNumber = LastEmittedEventNumber + 1});
            }
            public int LastEmittedEventNumber { get; set; }
            public void Apply(AnEvent e)
            {
                LastEmittedEventNumber = e.EventNumber;
            }
        }

        public class AnEvent : DomainEvent<MyRoot>
        {
            public int EventNumber { get; set; }
        }

        class MyViewInstance : IViewInstance<InstancePerAggregateRootLocator>, ISubscribeTo<AnEvent>
        {
            public MyViewInstance()
            {
                Calls = new List<Tuple<int, int>>();
            }
            public string Id { get; set; }
            public long LastGlobalSequenceNumber { get; set; }
            public List<Tuple<int,int>> Calls { get; set; }
            public void Handle(IViewContext context, AnEvent domainEvent)
            {
                var myRoot = context.Load<MyRoot>(domainEvent.GetAggregateRootId(), domainEvent.GetGlobalSequenceNumber());

                Calls.Add(Tuple.Create(myRoot.LastEmittedEventNumber, domainEvent.EventNumber));
            }
        }

        class MyViewInstanceImplicit : IViewInstance<InstancePerAggregateRootLocator>, ISubscribeTo<AnEvent>
        {
            public MyViewInstanceImplicit()
            {
                Calls = new List<Tuple<int, int>>();
            }
            public string Id { get; set; }
            public long LastGlobalSequenceNumber { get; set; }
            public List<Tuple<int,int>> Calls { get; set; }
            public void Handle(IViewContext context, AnEvent domainEvent)
            {
                var myRoot = context.Load<MyRoot>(domainEvent.GetAggregateRootId());

                Calls.Add(Tuple.Create(myRoot.LastEmittedEventNumber, domainEvent.EventNumber));
            }
        }
    }
}
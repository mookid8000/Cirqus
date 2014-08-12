using System;
using System.Collections.Generic;
using System.Linq;
using d60.Circus.Aggregates;
using d60.Circus.Commands;
using d60.Circus.Config;
using d60.Circus.Events;
using d60.Circus.Extensions;
using d60.Circus.TestHelpers.Internals;
using d60.Circus.Views.Basic;
using d60.Circus.Views.Basic.Locators;
using NUnit.Framework;

namespace d60.Circus.Tests.Views
{
    [TestFixture]
    public class TestLoadAggregatesFromView : FixtureBase
    {
        CommandProcessor _circus;
        InMemoryViewManager<MyViewInstance> _viewManager;

        protected override void DoSetUp()
        {
            var eventStore = new InMemoryEventStore();
            _viewManager = new InMemoryViewManager<MyViewInstance>();

            var basicAggregateRootRepository = new DefaultAggregateRootRepository(eventStore);

            _circus = new CommandProcessor(eventStore, basicAggregateRootRepository, new CommandMapper(), new BasicEventDispatcher(basicAggregateRootRepository, _viewManager));

            _circus.Initialize();
        }

        [Test]
        public void CanAccessAggregateRootInView()
        {
            var aggregateRootId = Guid.NewGuid();

            _circus.ProcessCommand(new MyCommand(aggregateRootId));
            _circus.ProcessCommand(new MyCommand(aggregateRootId));
            _circus.ProcessCommand(new MyCommand(aggregateRootId));
            _circus.ProcessCommand(new MyCommand(aggregateRootId));
            _circus.ProcessCommand(new MyCommand(aggregateRootId));

            var view = _viewManager.Load(InstancePerAggregateRootLocator.GetViewIdFromGuid(aggregateRootId));
            
            Assert.That(view.Calls.All(c => c.Item1 == c.Item2), "Registered calls contained a call where the version of the loaded aggregate root did not correspond to the version of the event that the view got to process: {0}",
                string.Join(", ", view.Calls.Select(c => string.Format("{0}/{1}", c.Item1, c.Item2))));
        }

        class MyCommand : MappedCommand<MyRoot>
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
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using d60.Cirqus.Aggregates;
using d60.Cirqus.Commands;
using d60.Cirqus.Events;
using d60.Cirqus.Extensions;
using d60.Cirqus.Logging;
using d60.Cirqus.Testing.Internals;
using d60.Cirqus.Tests.Stubs;
using d60.Cirqus.Views;
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
        InMemoryViewManager<MyViewInstanceEmitting> _viewManager3;
        ViewManagerEventDispatcher _eventDispatcher;
        InMemoryViewManager<MyViewInstanceLoadingNonexistentRoot> _viewManager4;

        protected override void DoSetUp()
        {
            var eventStore = new InMemoryEventStore();

            _viewManager1 = new InMemoryViewManager<MyViewInstance>();
            _viewManager2 = new InMemoryViewManager<MyViewInstanceImplicit>();
            _viewManager3 = new InMemoryViewManager<MyViewInstanceEmitting>();
            _viewManager4 = new InMemoryViewManager<MyViewInstanceLoadingNonexistentRoot>();

            var basicAggregateRootRepository = new DefaultAggregateRootRepository(eventStore);

            _eventDispatcher = new ViewManagerEventDispatcher(basicAggregateRootRepository, eventStore);

            _cirqus = new CommandProcessor(eventStore, basicAggregateRootRepository, _eventDispatcher);

            _cirqus.Initialize();

            RegisterForDisposal(_cirqus);
        }

        [Test]
        public void ExceptionIsThrownIfAggregateRootDoesNotExist()
        {
            _eventDispatcher.AddViewManager(_viewManager4);

            var listLoggerFactory = new ListLoggerFactory();
            CirqusLoggerFactory.Current = listLoggerFactory;

            _cirqus.ProcessCommand(new MyCommand(new Guid("FDC3AF8E-8E89-4129-9D76-33D1B461F40E")));

            Thread.Sleep(300);

            var relevantLines = listLoggerFactory
                .LoggedLines
                .Where(l => l.Level > Logger.Level.Info)
                .ToList();

            var stringWithTheLines = string.Join(Environment.NewLine, relevantLines);

            Console.WriteLine("---------------------------------------------");
            Console.WriteLine(stringWithTheLines);
            Console.WriteLine("---------------------------------------------");

            Assert.That(stringWithTheLines.ToLowerInvariant(), Contains.Substring("exception"));
            Assert.That(stringWithTheLines.ToLowerInvariant(), Contains.Substring("does not exist"));
        }

        [Test]
        public void ExceptionIsThrownIfAggregateRootEmitsFromView()
        {
            _eventDispatcher.AddViewManager(_viewManager3);

            var listLoggerFactory = new ListLoggerFactory();
            CirqusLoggerFactory.Current = listLoggerFactory;
            var aggregateRootId = Guid.NewGuid();

            var result = _cirqus.ProcessCommand(new MyCommand(aggregateRootId));

            Thread.Sleep(300);

            var relevantLines = listLoggerFactory
                .LoggedLines
                .Where(l => l.Level > Logger.Level.Info)
                .ToList();

            var stringWithTheLines = string.Join(Environment.NewLine, relevantLines);

            Console.WriteLine("---------------------------------------------");
            Console.WriteLine(stringWithTheLines);
            Console.WriteLine("---------------------------------------------");

            Assert.That(stringWithTheLines.ToLowerInvariant(), Contains.Substring("frozen"));
            Assert.That(stringWithTheLines.ToLowerInvariant(), Contains.Substring("exception"));
        }


        [Test]
        public void CanAccessAggregateRootInView()
        {
            _eventDispatcher.AddViewManager(_viewManager1);

            var aggregateRootId = Guid.NewGuid();

            _cirqus.ProcessCommand(new MyCommand(aggregateRootId));
            _cirqus.ProcessCommand(new MyCommand(aggregateRootId));
            _cirqus.ProcessCommand(new MyCommand(aggregateRootId));
            _cirqus.ProcessCommand(new MyCommand(aggregateRootId));
            var lastResult = _cirqus.ProcessCommand(new MyCommand(aggregateRootId));

            _eventDispatcher.WaitUntilProcessed(lastResult, TimeSpan.FromSeconds(3)).Wait();

            var view = _viewManager1.Load(InstancePerAggregateRootLocator.GetViewIdFromAggregateRootId(aggregateRootId));

            Assert.That(view.Calls.All(c => c.Item1 == c.Item2), "Registered calls contained a call where the version of the loaded aggregate root did not correspond to the version of the event that the view got to process: {0}",
                string.Join(", ", view.Calls.Select(c => string.Format("{0}/{1}", c.Item1, c.Item2))));
        }

        [Test]
        public void CanAccessAggregateRootInViewWithImplicitDeductionOfGlobalSequenceNumberInView()
        {
            _eventDispatcher.AddViewManager(_viewManager2);

            var aggregateRootId = Guid.NewGuid();

            _cirqus.ProcessCommand(new MyCommand(aggregateRootId));
            _cirqus.ProcessCommand(new MyCommand(aggregateRootId));
            _cirqus.ProcessCommand(new MyCommand(aggregateRootId));
            _cirqus.ProcessCommand(new MyCommand(aggregateRootId));
            
            var lastResult = _cirqus.ProcessCommand(new MyCommand(aggregateRootId));

            _eventDispatcher.WaitUntilProcessed(lastResult, TimeSpan.FromSeconds(3)).Wait();

            var view = _viewManager2.Load(InstancePerAggregateRootLocator.GetViewIdFromAggregateRootId(aggregateRootId));

            Assert.That(view, Is.Not.Null);

            Assert.That(view.Calls.All(c => c.Item1 == c.Item2), "Registered calls contained a call where the version of the loaded aggregate root did not correspond to the version of the event that the view got to process: {0}",
                string.Join(", ", view.Calls.Select(c => string.Format("{0}/{1}", c.Item1, c.Item2))));
        }

        class MyCommand : Command<MyRoot>
        {
            public MyCommand(Guid aggregateRootId)
                : base(aggregateRootId)
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
                Emit(new AnEvent { EventNumber = LastEmittedEventNumber + 1 });
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
            public List<Tuple<int, int>> Calls { get; set; }
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
            public List<Tuple<int, int>> Calls { get; set; }
            public void Handle(IViewContext context, AnEvent domainEvent)
            {
                var myRoot = context.Load<MyRoot>(domainEvent.GetAggregateRootId());

                Calls.Add(Tuple.Create(myRoot.LastEmittedEventNumber, domainEvent.EventNumber));
            }
        }

        class MyViewInstanceEmitting : IViewInstance<InstancePerAggregateRootLocator>, ISubscribeTo<AnEvent>
        {
            public string Id { get; set; }
            
            public long LastGlobalSequenceNumber { get; set; }
            
            public void Handle(IViewContext context, AnEvent domainEvent)
            {
                var root = context.Load<MyRoot>(domainEvent.GetAggregateRootId());

                root.DoStuff();
            }
        }

        class MyViewInstanceLoadingNonexistentRoot : IViewInstance<InstancePerAggregateRootLocator>, ISubscribeTo<AnEvent>
        {
            public string Id { get; set; }
            
            public long LastGlobalSequenceNumber { get; set; }
            
            public void Handle(IViewContext context, AnEvent domainEvent)
            {
                var randomGuid = new Guid("6E421AEB-44E0-45E7-8EFF-744943C5106C");

                var root = context.Load<MyRoot>(randomGuid);
            }
        }
    }
}
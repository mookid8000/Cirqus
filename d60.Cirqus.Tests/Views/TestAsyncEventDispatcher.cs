using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using d60.Cirqus.Aggregates;
using d60.Cirqus.Commands;
using d60.Cirqus.Config;
using d60.Cirqus.Events;
using d60.Cirqus.TestHelpers.Internals;
using d60.Cirqus.Tests.Stubs;
using d60.Cirqus.Views;
using d60.Cirqus.Views.ViewManagers;
using d60.Cirqus.Views.ViewManagers.Old;
using NUnit.Framework;

namespace d60.Cirqus.Tests.Views
{
    [TestFixture]
    public class TestAsyncEventDispatcher : FixtureBase
    {
        CommandProcessor _processor;

        [TestCase(true)]
        [TestCase(false)]
        public void ItWorks(bool makeAsync)
        {
            var listLoggerFactory = new ListLoggerFactory();
            var eventStore = new InMemoryEventStore();
            var aggregateRootRepository = new DefaultAggregateRootRepository(eventStore);

            IEventDispatcher eventDispatcher = new ViewManagerEventDispatcher(aggregateRootRepository, new SlowViewManager());

            if (makeAsync)
            {
                eventDispatcher = eventDispatcher.Asynchronous();
            }

            _processor = new CommandProcessor(eventStore, aggregateRootRepository, eventDispatcher)
            {
                Options =
                {
                    GlobalLoggerFactory = listLoggerFactory
                }
            }.Initialize();

            RegisterForDisposal(_processor);

            Console.WriteLine("Processing a command (async = {0})", makeAsync);
            var stopwatch = Stopwatch.StartNew();
            2.Times(() => _processor.ProcessCommand(new MyCommand(Guid.NewGuid())));
            var elapsedSecondsProcessingCommands = stopwatch.Elapsed.TotalSeconds;

            Thread.Sleep(TimeSpan.FromSeconds(2.5));

            if (makeAsync)
            {
                Assert.That(elapsedSecondsProcessingCommands, Is.LessThan(1));
            }
            else
            {
                Assert.That(elapsedSecondsProcessingCommands, Is.GreaterThan(1.99));
            }
        }

        public class MyRoot : AggregateRoot, IEmit<MyEvent>
        {
            public void EmitSomething()
            {
                Emit(new MyEvent());
            }

            public void Apply(MyEvent e)
            {

            }
        }

        public class MyEvent : DomainEvent<MyRoot>
        {

        }

        public class MyCommand : Command<MyRoot>
        {
            public MyCommand(Guid aggregateRootId)
                : base(aggregateRootId)
            {
            }

            public override void Execute(MyRoot aggregateRoot)
            {
                aggregateRoot.EmitSomething();
            }
        }

        class SlowViewManager : IViewManager, IPushViewManager
        {
            public void Initialize(IViewContext context, IEventStore eventStore, bool purgeExistingViews = false)
            {
                Console.WriteLine("Initializing....");
            }

            public bool Stopped { get; set; }

            public void Dispatch(IViewContext context, IEventStore eventStore, IEnumerable<DomainEvent> events)
            {
                Thread.Sleep(TimeSpan.FromSeconds(1));
            }
        }
    }
}
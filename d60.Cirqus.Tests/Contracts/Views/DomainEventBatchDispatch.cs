using System;
using System.Collections.Generic;
using System.Linq;
using d60.Cirqus.Aggregates;
using d60.Cirqus.Commands;
using d60.Cirqus.Events;
using d60.Cirqus.Extensions;
using d60.Cirqus.Logging;
using d60.Cirqus.Logging.Console;
using d60.Cirqus.Tests.Contracts.Views.Factories;
using d60.Cirqus.Tests.Extensions;
using d60.Cirqus.Views.ViewManagers;
using d60.Cirqus.Views.ViewManagers.Locators;
using NUnit.Framework;

namespace d60.Cirqus.Tests.Contracts.Views
{
    [TestFixture(typeof(MongoDbViewManagerFactory), Category = TestCategories.MongoDb)]
    [TestFixture(typeof(PostgreSqlViewManagerFactory), Category = TestCategories.PostgreSql)]
    [TestFixture(typeof(MsSqlViewManagerFactory), Category = TestCategories.MsSql)]
    [TestFixture(typeof(EntityFrameworkViewManagerFactory), Category = TestCategories.MsSql)]
    [TestFixture(typeof(InMemoryViewManagerFactory))]
    [TestFixture(typeof(HybridDbViewManagerFactory), Category = TestCategories.MsSql)]
    public class DomainEventBatchDispatch<TFactory> : FixtureBase where TFactory : AbstractViewManagerFactory, new()
    {
        TFactory _factory;
        ICommandProcessor _commandProcessor;
        ViewManagerWaitHandle _waitHandle;
        IViewManager<BatchView> _viewManager;

        protected override void DoSetUp()
        {
            CirqusLoggerFactory.Current = new ConsoleLoggerFactory(minLevel: Logger.Level.Debug);

            _factory = RegisterForDisposal(new TFactory());

            _viewManager = _factory.GetViewManager<BatchView>(enableBatchDispatch: true);

            _waitHandle = new ViewManagerWaitHandle();

            _commandProcessor = CommandProcessor.With()
                .EventStore(e => e.UseInMemoryEventStore())
                .EventDispatcher(e =>
                {
                    e.UseViewManagerEventDispatcher(_viewManager)
                        .WithWaitHandle(_waitHandle);
                })
                .Create();

            RegisterForDisposal(_commandProcessor);
        }

        [Test]
        public void EventsCanBeDispatchedToViewLocatorAndViewInBatches()
        {
            var result = _commandProcessor.ProcessCommand(new DoStuffCommand("mogens", 20));

            _waitHandle.WaitForAll(result, TimeSpan.FromSeconds(30)).Wait();

            var viewInstance = _viewManager.Load("mogens");

            Assert.That(viewInstance, Is.Not.Null);
            Assert.That(viewInstance.NumberOfCalls, Is.EqualTo(1));
        }

        class Root : AggregateRoot, IEmit<Event>
        {
            int _eventNumberEmitted;

            public void DoStuff()
            {
                Emit(new Event(_eventNumberEmitted + 1));
            }

            public void Apply(Event e)
            {
                _eventNumberEmitted = e.EventNumber;
            }
        }

        class Event : DomainEvent<Root>
        {
            public int EventNumber { get; private set; }

            public Event(int eventNumber)
            {
                EventNumber = eventNumber;
            }
        }

        class DoStuffCommand : Command<Root>
        {
            public int NumberOfCallsToMake { get; private set; }

            public DoStuffCommand(string aggregateRootId, int numberOfCallsToMake)
                : base(aggregateRootId)
            {
                NumberOfCallsToMake = numberOfCallsToMake;
            }

            public override void Execute(Root aggregateRoot)
            {
                NumberOfCallsToMake.Times(aggregateRoot.DoStuff);
            }
        }
    }

   public class BatchView : IViewInstance<BatchViewLocator>, ISubscribeTo<DomainEventBatch>
    {
        public string Id { get; set; }
        public long LastGlobalSequenceNumber { get; set; }
        public int NumberOfCalls { get; set; }
        public void Handle(IViewContext context, DomainEventBatch domainEvent)
        {
            NumberOfCalls++;
        }
    }

    class BatchViewLocator : ViewLocator
    {
        protected override IEnumerable<string> GetViewIds(IViewContext context, DomainEvent e)
        {
            var domainEventBatch = e as DomainEventBatch;

            if (domainEventBatch == null)
                return new string[0];

            return domainEventBatch
                .Select(d => d.GetAggregateRootId())
                .Distinct();
        }
    }
}
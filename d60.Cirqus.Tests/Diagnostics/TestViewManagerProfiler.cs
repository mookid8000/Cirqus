using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using d60.Cirqus.Aggregates;
using d60.Cirqus.Commands;
using d60.Cirqus.Events;
using d60.Cirqus.Logging;
using d60.Cirqus.Tests.Extensions;
using d60.Cirqus.Views;
using d60.Cirqus.Views.ViewManagers;
using d60.Cirqus.Views.ViewManagers.Locators;
using NUnit.Framework;

namespace d60.Cirqus.Tests.Diagnostics
{
    [TestFixture]
    public class TestViewManagerProfiler : FixtureBase
    {
        [Test]
        public async Task CanMeasureTimeSpentInViewManagers()
        {
            var waitHandle = new ViewManagerWaitHandle();
            var myProfiler = new MyProfiler();
            var view1 = new InMemoryViewManager<SlowView>();
            var view2 = new InMemoryViewManager<QuickView>();

            var commandProcessor = CommandProcessor.With()
                .Logging(l => l.UseConsole(minLevel: Logger.Level.Debug))
                .EventStore(e => e.UseInMemoryEventStore())
                .EventDispatcher(e =>
                {
                    e.UseViewManagerEventDispatcher(view1, view2)
                        .WithWaitHandle(waitHandle)
                        .WithProfiler(myProfiler);
                })
                .Create();

            using (commandProcessor)
            {
                var lastResult = Enumerable.Range(0, 10)
                    .Select(i => commandProcessor.ProcessCommand(new Commando("someId")))
                    .Last();

                await waitHandle.WaitForAll(lastResult, TimeSpan.FromMinutes(1));
            }

            var accumulatedTimes = myProfiler.GetAccumulatedTimes();

            Assert.That(accumulatedTimes.ContainsKey(view1), "Could not find {0} among the keys!", view1);
            Assert.That(accumulatedTimes.ContainsKey(view2), "Could not find {0} among the keys!", view2);

            Assert.That(accumulatedTimes[view1], Is.GreaterThan(TimeSpan.FromSeconds(1)));
            Assert.That(accumulatedTimes[view2], Is.GreaterThan(TimeSpan.FromSeconds(.1)).And.LessThan(TimeSpan.FromSeconds(0.15)));
        }

        class MyProfiler : IViewManagerProfiler
        {
            readonly ConcurrentDictionary<IViewManager, TimeSpan> _timeSpent = new ConcurrentDictionary<IViewManager, TimeSpan>();

            public void RegisterTimeSpent(IViewManager viewManager, DomainEvent domainEvent, TimeSpan duration)
            {
                _timeSpent.AddOrUpdate(viewManager, _ => duration, (_, value) => value + duration);
            }

            public Dictionary<IViewManager, TimeSpan> GetAccumulatedTimes()
            {
                return _timeSpent.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            }
        }

        class SlowView : IViewInstance<InstancePerAggregateRootLocator>, ISubscribeTo<Event>
        {
            public string Id { get; set; }
            public long LastGlobalSequenceNumber { get; set; }

            public void Handle(IViewContext context, Event domainEvent)
            {
                Thread.Sleep(100);
            }
        }

        class QuickView : IViewInstance<InstancePerAggregateRootLocator>, ISubscribeTo<Event>
        {
            public string Id { get; set; }
            public long LastGlobalSequenceNumber { get; set; }

            public void Handle(IViewContext context, Event domainEvent)
            {
                Thread.Sleep(10);
            }
        }

        class Commando : Command<Root>
        {
            public Commando(string aggregateRootId)
                : base(aggregateRootId)
            {
            }

            public override void Execute(Root aggregateRoot)
            {
                aggregateRoot.DoStuff();
            }
        }

        class Root : AggregateRoot, IEmit<Event>
        {
            public void DoStuff()
            {
                Emit(new Event());
            }

            public void Apply(Event e)
            {
            }
        }

        class Event : DomainEvent<Root>
        {
        }
    }
}
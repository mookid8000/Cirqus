using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using d60.Cirqus.Aggregates;
using d60.Cirqus.Commands;
using d60.Cirqus.Events;
using d60.Cirqus.Extensions;
using d60.Cirqus.MsSql.Views;
using d60.Cirqus.Snapshotting;
using d60.Cirqus.Views.ViewManagers;
using d60.Cirqus.Views.ViewManagers.Locators;

namespace d60.Cirqus.Tests.Snapshotting.Models
{
    [EnableSnapshots(1)]
    public class Root : AggregateRoot, IEmit<RootGotNewNumber>
    {
        int _number;

        public void Increment()
        {
            Emit(new RootGotNewNumber(_number + 1));
        }

        public void Apply(RootGotNewNumber e)
        {
            _number = e.NewNumber;
        }

        public int GetNumber()
        {
            return _number;
        }
    }

    public class RootGotNewNumber : DomainEvent<Root>
    {
        public int NewNumber { get; private set; }

        public RootGotNewNumber(int newNumber)
        {
            NewNumber = newNumber;
        }
    }

    public class IncrementRoot : Command<Root>
    {
        public IncrementRoot(string aggregateRootId) : base(aggregateRootId)
        {
        }

        public override void Execute(Root aggregateRoot)
        {
            aggregateRoot.Increment();
        }
    }

    public class RootNumberView : IViewInstance<GlobalInstanceLocator>, ISubscribeTo<RootGotNewNumber>
    {
        public RootNumberView()
        {
            NumbersByRoot = new Dictionary<string, int>();
        }
        public string Id { get; set; }
        public long LastGlobalSequenceNumber { get; set; }

        [Json]
        public Dictionary<string, int> NumbersByRoot { get; set; }

        public void Handle(IViewContext context, RootGotNewNumber domainEvent)
        {
            var stopwatch = Stopwatch.StartNew();
            var dispatchTime = DateTime.UtcNow;

            var aggregateRootId = domainEvent.GetAggregateRootId();
            var root = context.Load<Root>(aggregateRootId);
            NumbersByRoot[aggregateRootId] = root.GetNumber();

            var stats = (ConcurrentQueue<DispatchStats>)context.Items["stats"];
            stats.Enqueue(new DispatchStats(dispatchTime, stopwatch.Elapsed));
        }
    }

    public class DispatchStats
    {
        public DateTime Time { get; private set; }
        public TimeSpan Elapsed { get; private set; }

        public DispatchStats(DateTime time, TimeSpan elapsed)
        {
            Time = time;
            Elapsed = elapsed;
        }
    }
}
using System;
using d60.Cirqus.Extensions;
using d60.Cirqus.Views.ViewManagers;
using d60.Cirqus.Views.ViewManagers.Locators;

namespace d60.Cirqus.Tests.Bugs.Scenario
{
    public class CountingRootView : IViewInstance<InstancePerAggregateRootLocator>, ISubscribeTo<CountingRootIncremented>
    {
        public string Id { get; set; }
        public long LastGlobalSequenceNumber { get; set; }
        public int Number { get; set; }
        public void Handle(IViewContext context, CountingRootIncremented domainEvent)
        {
            Console.WriteLine("Loading aggregate root");
            var root = context.Load<CountingRoot>(domainEvent.GetAggregateRootId());

            Console.WriteLine($"Got the number {root.Number} from it");
            Number = root.Number;
        }
    }
}
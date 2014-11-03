using System;
using d60.Cirqus.Extensions;
using d60.Cirqus.Views.ViewManagers;
using d60.Cirqus.Views.ViewManagers.Locators;

namespace d60.Cirqus.Tests.Contracts.Views.Models.UpdatedEvent
{
    public class View : IViewInstance<InstancePerAggregateRootLocator>, ISubscribeTo<Event>
    {
        public string Id { get; set; }
        public long LastGlobalSequenceNumber { get; set; }
        public int EventCount { get; set; }
        public string AggregateRootId { get; set; }
        public void Handle(IViewContext context, Event domainEvent)
        {
            AggregateRootId = domainEvent.GetAggregateRootId();
            EventCount++;
        }
    }
}
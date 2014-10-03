using d60.Cirqus.Views.ViewManagers;
using d60.Cirqus.Views.ViewManagers.Locators;

namespace d60.Cirqus.Tests.Contracts.Views.Models.PolymorphicDispatch
{
    public class ViewThatSubscribesToEvents : IViewInstance<InstancePerAggregateRootLocator>,
        ISubscribeTo<Event>,
        ISubscribeTo<AnotherEvent>
    {
        public string Id { get; set; }
        
        public long LastGlobalSequenceNumber { get; set; }

        public int ProcessedEvents { get; set; }

        public void Handle(IViewContext context, Event domainEvent)
        {
            ProcessedEvents++;
        }

        public void Handle(IViewContext context, AnotherEvent domainEvent)
        {
            ProcessedEvents++;
        }
    }
}
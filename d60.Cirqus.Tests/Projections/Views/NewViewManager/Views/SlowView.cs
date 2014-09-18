using System.Threading;
using d60.Cirqus.Projections.Views.ViewManagers;
using d60.Cirqus.Projections.Views.ViewManagers.Locators;
using d60.Cirqus.Tests.Projections.Views.NewViewManager.Events;

namespace d60.Cirqus.Tests.Projections.Views.NewViewManager.Views
{
    public class SlowView : IViewInstance<InstancePerAggregateRootLocator>,
        ISubscribeTo<PotatoCreated>,
        ISubscribeTo<WasEaten>
    {
        public string Id { get; set; }
        
        public long LastGlobalSequenceNumber { get; set; }
        
        public void Handle(IViewContext context, PotatoCreated domainEvent)
        {
            Thread.Sleep(100);
        }

        public void Handle(IViewContext context, WasEaten domainEvent)
        {
            Thread.Sleep(100);
        }
    }
}
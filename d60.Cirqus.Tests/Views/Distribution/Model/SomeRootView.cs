using System;
using System.Threading;
using d60.Cirqus.Views.ViewManagers;
using d60.Cirqus.Views.ViewManagers.Locators;

namespace d60.Cirqus.Tests.Views.Distribution.Model
{
    public class SomeRootView : IViewInstance<InstancePerAggregateRootLocator>, ISubscribeTo<DidStuff>
    {
        public string Id { get; set; }
        public long LastGlobalSequenceNumber { get; set; }
        public int HowManyThingsDidItDo { get; set; }
        public void Handle(IViewContext context, DidStuff domainEvent)
        {
            Console.Write(".");
            
            Thread.Sleep(1000);
            
            HowManyThingsDidItDo ++;
            
            Console.Write("!");
        }
    }
}
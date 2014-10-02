using System;
using System.Collections.Generic;
using d60.Cirqus.Views.ViewManagers;
using d60.Cirqus.Views.ViewManagers.Locators;

namespace d60.Cirqus.Tests.Contracts.Views.Models.GeneralViewManagerTest
{
    public class GeneratedIds : IViewInstance<InstancePerAggregateRootLocator>, ISubscribeTo<IdGenerated>
    {
        public string Id { get; set; }
        public long LastGlobalSequenceNumber { get; set; }

        public GeneratedIds()
        {
            AllIds = new HashSet<string>();
        }

        public HashSet<string> AllIds { get; set; }
        
        public void Handle(IViewContext context, IdGenerated domainEvent)
        {
            Console.WriteLine("=============== Adding ID: {0} ===============", domainEvent.GetId());

            AllIds.Add(domainEvent.GetId());
        }
    }
}
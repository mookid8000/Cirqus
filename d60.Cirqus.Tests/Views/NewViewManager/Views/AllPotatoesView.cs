using System;
using System.Collections.Generic;
using d60.Cirqus.Extensions;
using d60.Cirqus.Tests.Views.NewViewManager.Events;
using d60.Cirqus.Views.ViewManagers;
using d60.Cirqus.Views.ViewManagers.Locators;

namespace d60.Cirqus.Tests.Views.NewViewManager.Views
{
    public class AllPotatoesView : IViewInstance<GlobalInstanceLocator>, ISubscribeTo<PotatoCreated>
    {
        public AllPotatoesView()
        {
            NamesOfPotatoes = new Dictionary<Guid, string>();
        }

        public string Id { get; set; }

        public long LastGlobalSequenceNumber { get; set; }

        public Dictionary<Guid, string> NamesOfPotatoes { get; set; }

        public void Handle(IViewContext context, PotatoCreated domainEvent)
        {
            NamesOfPotatoes[domainEvent.GetAggregateRootId()] = domainEvent.Name;
        }
    }
}
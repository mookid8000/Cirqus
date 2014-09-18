using System;
using System.Collections.Generic;
using d60.Cirqus.Extensions;
using d60.Cirqus.Projections.Views.ViewManagers;
using d60.Cirqus.Projections.Views.ViewManagers.Locators;
using d60.Cirqus.Tests.Projections.Views.NewViewManager.Events;

namespace d60.Cirqus.Tests.Projections.Views.NewViewManager.Views
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
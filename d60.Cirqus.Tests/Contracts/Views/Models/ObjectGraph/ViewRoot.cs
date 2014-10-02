using System;
using System.Collections.Generic;
using System.Linq;
using d60.Cirqus.Views.ViewManagers;
using d60.Cirqus.Views.ViewManagers.Locators;

namespace d60.Cirqus.Tests.Contracts.Views.Models.ObjectGraph
{
    public class ViewRoot : IViewInstance<InstancePerAggregateRootLocator>, ISubscribeTo<Event>
    {
        public ViewRoot()
        {
            Children = new List<ViewChild>();
        }

        public string Id { get; set; }

        public long LastGlobalSequenceNumber { get; set; }

        public virtual List<ViewChild> Children { get; set; }

        public void Handle(IViewContext context, Event domainEvent)
        {
            while (Children.Count < domainEvent.NumberOfChildren) AddChild();
            while (Children.Count > domainEvent.NumberOfChildren) RemoveChild();
        }

        void AddChild()
        {
            Children.Add(new ViewChild { Something = "klokken er " + DateTime.Now });
        }

        void RemoveChild()
        {
            Children.Remove(Children.Last());
        }
    }
}
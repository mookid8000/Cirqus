using System;
using System.Collections.Generic;
using d60.Cirqus.Aggregates;
using d60.Cirqus.Events;

namespace d60.Cirqus.Views.ViewManagers.Locators
{
    /// <summary>
    /// Scopes the view instance to the aggregate root
    /// </summary>
    public class InstancePerAggregateRootLocator : ViewLocator
    {
        protected override IEnumerable<string> GetViewIds(IViewContext context, DomainEvent e)
        {
            return new[] {e.Meta[DomainEvent.MetadataKeys.AggregateRootId].ToString()};
        }

        public static string GetViewIdFromAggregateRoot(AggregateRoot aggregateRoot)
        {
            return GetViewIdFromAggregateRootId(aggregateRoot.Id);
        }

        public static string GetViewIdFromAggregateRootId(string id)
        {
            return id;
        }
    }
}
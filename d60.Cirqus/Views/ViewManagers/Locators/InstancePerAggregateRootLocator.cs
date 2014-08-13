using System;
using d60.Cirqus.Aggregates;
using d60.Cirqus.Events;

namespace d60.Cirqus.Views.ViewManagers.Locators
{
    /// <summary>
    /// Scopes the view instance to the aggregate root
    /// </summary>
    public class InstancePerAggregateRootLocator : ViewLocator
    {
        public override string GetViewId(DomainEvent e)
        {
            return e.Meta[DomainEvent.MetadataKeys.AggregateRootId].ToString();
        }

        public static string GetViewIdFromAggregateRoot(AggregateRoot aggregateRoot)
        {
            return GetViewIdFromGuid(aggregateRoot.Id);
        }

        public static string GetViewIdFromGuid(Guid guid)
        {
            return guid.ToString();
        }
    }
}
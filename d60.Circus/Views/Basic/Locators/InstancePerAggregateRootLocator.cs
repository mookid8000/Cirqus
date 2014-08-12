using System;
using d60.Circus.Aggregates;
using d60.Circus.Events;

namespace d60.Circus.Views.Basic.Locators
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
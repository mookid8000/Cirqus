using d60.EventSorcerer.Events;

namespace d60.EventSorcerer.Views.Basic.Locators
{
    public class InstancePerAggregateRootLocator : ViewLocator
    {
        public override string GetViewId(DomainEvent e)
        {
            return e.Meta[DomainEvent.MetadataKeys.AggregateRootId].ToString();
        }
    }
}
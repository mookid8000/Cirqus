using d60.EventSorcerer.Events;

namespace d60.EventSorcerer.Views.Basic.Locators
{
    public class InstancePerDomainEventTypeLocator : ViewLocator
    {
        public override string GetViewId(DomainEvent e)
        {
            return e.GetType().FullName;
        }
    }
}
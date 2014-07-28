using d60.EventSorcerer.Events;

namespace d60.EventSorcerer.Views.Basic.Locators
{
    public class GlobalInstanceLocator : ViewLocator
    {
        public override string GetViewId(DomainEvent e)
        {
            return "__global__";
        }
    }
}
using d60.EventSorcerer.Events;

namespace d60.EventSorcerer.Views.Basic.Locators
{
    public class GlobalInstanceLocator : ViewLocator
    {
        const string ViewInstanceId = "__global__";

        public override string GetViewId(DomainEvent e)
        {
            return ViewInstanceId;
        }

        public static string GetViewInstanceId()
        {
            return ViewInstanceId;
        }
    }
}
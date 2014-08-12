using d60.Circus.Events;

namespace d60.Circus.Views.Basic.Locators
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
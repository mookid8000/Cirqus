using d60.Cirqus.Events;

namespace d60.Cirqus.Views.ViewManagers.Locators
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
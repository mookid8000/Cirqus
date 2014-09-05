using System.Collections.Generic;
using d60.Cirqus.Events;

namespace d60.Cirqus.Views.ViewManagers.Locators
{
    public class GlobalInstanceLocator : ViewLocator
    {
        const string ViewInstanceId = "__global__";
        
        static readonly string[] ViewInstanceIds = { ViewInstanceId };

        public override IEnumerable<string> GetViewIds(DomainEvent e)
        {
            return ViewInstanceIds;
        }

        public static string GetViewInstanceId()
        {
            return ViewInstanceId;
        }
    }
}
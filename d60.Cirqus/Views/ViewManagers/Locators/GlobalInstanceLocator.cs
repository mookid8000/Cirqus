using System.Collections.Generic;
using d60.Cirqus.Events;

namespace d60.Cirqus.Views.ViewManagers.Locators
{
    /// <summary>
    /// View instance locator that always returns the same ID. This way, there will only ever be one instance of the view.
    /// </summary>
    public class GlobalInstanceLocator : ViewLocator
    {
        const string ViewInstanceId = "__global__";
        
        static readonly string[] ViewInstanceIds = { ViewInstanceId };

        protected override IEnumerable<string> GetViewIds(IViewContext context, DomainEvent e)
        {
            return ViewInstanceIds;
        }

        /// <summary>
        /// Gets the one ID that this locator will ever return
        /// </summary>
        public static string GetViewInstanceId()
        {
            return ViewInstanceId;
        }
    }
}
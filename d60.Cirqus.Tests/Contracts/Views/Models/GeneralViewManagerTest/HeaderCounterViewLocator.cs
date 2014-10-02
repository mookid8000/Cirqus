using System.Collections.Generic;
using System.Linq;
using d60.Cirqus.Events;
using d60.Cirqus.Views.ViewManagers;
using d60.Cirqus.Views.ViewManagers.Locators;

namespace d60.Cirqus.Tests.Contracts.Views.Models.GeneralViewManagerTest
{
    public class HeaderCounterViewLocator : ViewLocator
    {
        protected override IEnumerable<string> GetViewIds(IViewContext context, DomainEvent e)
        {
            return e.Meta.Keys.ToArray();
        }
    }
}
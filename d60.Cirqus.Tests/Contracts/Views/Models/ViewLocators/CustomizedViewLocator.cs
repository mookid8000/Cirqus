using System;
using System.Collections.Generic;
using d60.Cirqus.Events;
using d60.Cirqus.Views.ViewManagers;
using d60.Cirqus.Views.ViewManagers.Locators;

namespace d60.Cirqus.Tests.Contracts.Views.Models.ViewLocators
{
    class CustomizedViewLocator : ViewLocator
    {
        protected override IEnumerable<string> GetViewIds(IViewContext context, DomainEvent e)
        {
            if (e is JustAnEvent)
            {
                yield return "yay";
            }
            else
            {
                throw new ApplicationException("oh noes!!!!");
            }
        }
    }
}
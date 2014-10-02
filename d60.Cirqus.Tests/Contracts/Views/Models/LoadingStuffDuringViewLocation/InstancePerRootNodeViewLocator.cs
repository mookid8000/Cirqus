using System;
using System.Collections.Generic;
using d60.Cirqus.Events;
using d60.Cirqus.Extensions;
using d60.Cirqus.Views.ViewManagers;
using d60.Cirqus.Views.ViewManagers.Locators;

namespace d60.Cirqus.Tests.Contracts.Views.Models.LoadingStuffDuringViewLocation
{
    public class InstancePerRootNodeViewLocator : ViewLocator
    {
        protected override IEnumerable<string> GetViewIds(IViewContext context, DomainEvent e)
        {
            if (!(e is NodeAttachedToParentNode)) throw new ArgumentException(String.Format("Can't handle {0}", e));

            var node = context.Load<Node>(e.GetAggregateRootId());

            while (node.ParentNodeId != Guid.Empty)
            {
                node = context.Load<Node>(node.ParentNodeId);
            }

            return new[] { node.Id.ToString() };
        }
    }
}
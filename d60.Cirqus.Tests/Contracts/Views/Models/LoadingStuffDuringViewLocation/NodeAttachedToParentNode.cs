using System;
using d60.Cirqus.Events;

namespace d60.Cirqus.Tests.Contracts.Views.Models.LoadingStuffDuringViewLocation
{
    public class NodeAttachedToParentNode : DomainEvent<Node>
    {
        public Guid ParentNodeId { get; set; }
    }
}
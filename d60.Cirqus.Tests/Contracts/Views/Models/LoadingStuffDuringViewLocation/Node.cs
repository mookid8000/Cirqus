using System;
using d60.Cirqus.Aggregates;
using d60.Cirqus.Events;

namespace d60.Cirqus.Tests.Contracts.Views.Models.LoadingStuffDuringViewLocation
{
    public class Node : AggregateRoot, IEmit<NodeAttachedToParentNode>, IEmit<NodeCreated>
    {
        public Guid ParentNodeId { get; private set; }

        public void AttachTo(Node parentNode)
        {
            if (ParentNodeId != Guid.Empty)
            {
                throw new InvalidOperationException(string.Format("Cannot attach node {0} to {1} because it's already attached to {2}",
                    Id, parentNode.Id, ParentNodeId));
            }
            Emit(new NodeAttachedToParentNode { ParentNodeId = parentNode.Id });
        }

        public void Apply(NodeAttachedToParentNode e)
        {
            ParentNodeId = e.ParentNodeId;
        }

        protected override void Created()
        {
            Emit(new NodeCreated());
        }

        public void Apply(NodeCreated e)
        {
        }
    }
}
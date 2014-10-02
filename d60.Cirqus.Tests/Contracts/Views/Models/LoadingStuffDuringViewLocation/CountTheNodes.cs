using d60.Cirqus.Views.ViewManagers;

namespace d60.Cirqus.Tests.Contracts.Views.Models.LoadingStuffDuringViewLocation
{
    public class CountTheNodes : IViewInstance<InstancePerRootNodeViewLocator>, ISubscribeTo<NodeAttachedToParentNode> 
    {
        public string Id { get; set; }
        
        public long LastGlobalSequenceNumber { get; set; }
        
        public int Nodes { get; set; }
        
        public void Handle(IViewContext context, NodeAttachedToParentNode domainEvent)
        {
            Nodes++;
        }
    }
}
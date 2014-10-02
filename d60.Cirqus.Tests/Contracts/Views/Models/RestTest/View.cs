using d60.Cirqus.Views.ViewManagers;
using d60.Cirqus.Views.ViewManagers.Locators;

namespace d60.Cirqus.Tests.Contracts.Views.Models.RestTest
{
    /// <summary>
    /// View that does not subscribe to anything
    /// </summary>
    public class View : IViewInstance<InstancePerAggregateRootLocator>, ISubscribeTo
    {
        public string Id { get; set; }
        public long LastGlobalSequenceNumber { get; set; }
    }
}
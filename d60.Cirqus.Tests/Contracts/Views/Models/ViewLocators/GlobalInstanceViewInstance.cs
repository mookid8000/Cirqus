using d60.Cirqus.Views.ViewManagers;
using d60.Cirqus.Views.ViewManagers.Locators;

namespace d60.Cirqus.Tests.Contracts.Views.Models.ViewLocators
{
    class GlobalInstanceViewInstance : IViewInstance<GlobalInstanceLocator>, ISubscribeTo<ThisIsJustAnEvent>
    {
        public int EventCounter { get; set; }
        public void Handle(IViewContext context, ThisIsJustAnEvent domainEvent)
        {
            EventCounter++;
        }

        public string Id { get; set; }
        public long LastGlobalSequenceNumber { get; set; }
    }
}
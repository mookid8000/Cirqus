using d60.Cirqus.Views.ViewManagers;

namespace d60.Cirqus.Tests.Contracts.Views.Models.ViewLocators
{
    class MyViewInstance : IViewInstance<CustomizedViewLocator>, ISubscribeTo<JustAnEvent>
    {
        public string Id { get; set; }
        public long LastGlobalSequenceNumber { get; set; }

        public void Handle(IViewContext context, JustAnEvent domainEvent)
        {

        }
    }
}
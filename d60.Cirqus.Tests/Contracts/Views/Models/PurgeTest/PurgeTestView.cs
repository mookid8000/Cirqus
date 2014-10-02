using d60.Cirqus.Views.ViewManagers;
using d60.Cirqus.Views.ViewManagers.Locators;

namespace d60.Cirqus.Tests.Contracts.Views.Models.PurgeTest
{
    class PurgeTestView : IViewInstance<GlobalInstanceLocator>, ISubscribeTo<Event>
    {
        public static string StaticBadBoy { get; set; }

        public string Id { get; set; }

        public long LastGlobalSequenceNumber { get; set; }

        public string CaughtStaticBadBoy { get; set; }

        public void Handle(IViewContext context, Event domainEvent)
        {
            CaughtStaticBadBoy = StaticBadBoy;
        }
    }
}
using System.Collections.Generic;
using System.Threading;
using d60.Cirqus.Views.ViewManagers;
using d60.Cirqus.Views.ViewManagers.Locators;

namespace d60.Cirqus.Tests.Contracts.Views.Models.RecoveryTest
{
    public class View : IViewInstance<GlobalInstanceLocator>, ISubscribeTo<Event>
    {
        public static bool Fail { get; set; }

        public View()
        {
            EventIds = new List<int>();
        }

        public string Id { get; set; }

        public long LastGlobalSequenceNumber { get; set; }

        public List<int> EventIds { get; set; }

        public void Handle(IViewContext context, Event domainEvent)
        {
            if (Fail)
            {
                throw new BarrierPostPhaseException("oh noes!");
            }

            EventIds.Add(domainEvent.EventId);
            Thread.Sleep(10);
        }
    }
}
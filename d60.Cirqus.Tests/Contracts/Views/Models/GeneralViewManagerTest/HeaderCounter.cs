using System.Collections.Generic;
using d60.Cirqus.Views.ViewManagers;

namespace d60.Cirqus.Tests.Contracts.Views.Models.GeneralViewManagerTest
{
    public class HeaderCounter : IViewInstance<HeaderCounterViewLocator>, ISubscribeTo<AnEvent>
    {
        public HeaderCounter()
        {
            HeaderValues = new HashSet<string>();
        }

        public string Id { get; set; }
        
        public long LastGlobalSequenceNumber { get; set; }
        
        public HashSet<string> HeaderValues { get; set; }
        
        public void Handle(IViewContext context, AnEvent domainEvent)
        {
            var value = domainEvent.Meta[Id];

            HeaderValues.Add(value);
        }
    }
}
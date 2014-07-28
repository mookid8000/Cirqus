using System.Collections.Generic;
using d60.EventSorcerer.Events;

namespace d60.EventSorcerer.Tests.Stubs
{
    public class InMemoryEventCollector : IEventCollector
    {
        public readonly List<DomainEvent> EmittedEvents = new List<DomainEvent>();
        public void Add(DomainEvent e)
        {
            EmittedEvents.Add(e);
        }
    }
}
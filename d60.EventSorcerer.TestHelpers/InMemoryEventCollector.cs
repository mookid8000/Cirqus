using System.Collections;
using System.Collections.Generic;
using d60.EventSorcerer.Aggregates;
using d60.EventSorcerer.Events;

namespace d60.EventSorcerer.TestHelpers
{
    public class InMemoryEventCollector : IEventCollector, IEnumerable<DomainEvent>
    {
        readonly List<DomainEvent> _emittedEvents = new List<DomainEvent>();
        
        public void Add(DomainEvent e)
        {
            _emittedEvents.Add(e);
        }

        public InMemoryEventCollector CollectEventsFor(AggregateRoot aggregateRoot)
        {
            aggregateRoot.EventCollector = this;
            return this;
        }

        public IEnumerator<DomainEvent> GetEnumerator()
        {
            return _emittedEvents.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
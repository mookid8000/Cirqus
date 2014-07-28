using System.Collections.Generic;
using d60.EventSorcerer.Events;

namespace d60.EventSorcerer.Commands
{
    public class UnitOfWork : IEventCollector
    {
        readonly List<DomainEvent>  _emittedEvents = new List<DomainEvent>();

        public IEnumerable<DomainEvent> EmittedEvents
        {
            get { return _emittedEvents; }
        }

        public void Add(DomainEvent e)
        {
            _emittedEvents.Add(e);
        }

    }
}
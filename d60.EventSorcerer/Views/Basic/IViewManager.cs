using System.Collections.Generic;
using d60.EventSorcerer.Events;

namespace d60.EventSorcerer.Views.Basic
{
    public interface IViewManager
    {
        void Dispatch(IEventStore eventStore, IEnumerable<DomainEvent> events);
    }
}
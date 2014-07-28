using System.Collections.Generic;
using d60.EventSorcerer.Events;

namespace d60.EventSorcerer.Views
{
    public interface IViewManager
    {
        void Dispatch(IEnumerable<DomainEvent> events);
    }
}
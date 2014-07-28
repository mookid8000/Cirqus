using System.Collections.Generic;
using d60.EventSorcerer.Events;

namespace d60.EventSorcerer.Views.Basic
{
    public interface IViewDispatcher
    {
        void Dispatch(IEnumerable<DomainEvent> events);
    }
}
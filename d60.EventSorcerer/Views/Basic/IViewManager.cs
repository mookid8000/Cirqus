using System.Collections.Generic;
using d60.EventSorcerer.Events;

namespace d60.EventSorcerer.Views.Basic
{
    public interface IViewManager
    {
        void Initialize(IViewContext context, IEventStore eventStore, bool purgeExistingViews = false);
        void Dispatch(IViewContext context, IEventStore eventStore, IEnumerable<DomainEvent> events);
    }
}
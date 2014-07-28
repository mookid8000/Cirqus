using System.Collections.Generic;
using System.Linq;
using d60.EventSorcerer.Events;

namespace d60.EventSorcerer.Views.Basic
{
    public class BasicViewManager : IViewManager
    {
        readonly List<IViewDispatcher> _viewDispatchers;

        public BasicViewManager(IEnumerable<IViewDispatcher> viewDispatchers)
        {
            _viewDispatchers = viewDispatchers.ToList();
        }

        public void Dispatch(IEnumerable<DomainEvent> events)
        {
            var eventList = events.ToList();
            
            foreach (var view in _viewDispatchers)
            {
                view.Dispatch(eventList);
            }
        }
    }
}
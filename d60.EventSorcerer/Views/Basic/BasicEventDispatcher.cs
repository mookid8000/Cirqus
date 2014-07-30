using System.Collections.Generic;
using System.Linq;
using d60.EventSorcerer.Events;

namespace d60.EventSorcerer.Views.Basic
{
    public class BasicEventDispatcher : IEventDispatcher
    {
        readonly List<IViewManager> _viewDispatchers;

        public BasicEventDispatcher(IEnumerable<IViewManager> viewDispatchers)
        {
            _viewDispatchers = viewDispatchers.ToList();
        }

        public void Dispatch(IEventStore eventStore, IEnumerable<DomainEvent> events)
        {
            var eventList = events.ToList();
            
            foreach (var view in _viewDispatchers)
            {
                view.Dispatch(eventList);
            }
        }
    }
}
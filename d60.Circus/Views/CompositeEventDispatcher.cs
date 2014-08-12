using System.Collections.Generic;
using System.Linq;
using d60.Circus.Events;

namespace d60.Circus.Views
{
    /// <summary>
    /// Event dispatcher that can contain multiple event dispatchers
    /// </summary>
    public class CompositeEventDispatcher : IEventDispatcher
    {
        readonly List<IEventDispatcher> _eventDispatchers;

        public CompositeEventDispatcher(params IEventDispatcher[] eventDispatchers)
            :this((IEnumerable<IEventDispatcher>)eventDispatchers)
        {
        }

        public CompositeEventDispatcher(IEnumerable<IEventDispatcher> eventDispatchers)
        {
            _eventDispatchers = eventDispatchers.ToList();
        }

        public void Initialize(IEventStore eventStore, bool purgeExistingViews = false)
        {
            _eventDispatchers.ForEach(d => d.Initialize(eventStore, purgeExistingViews));
        }

        public void Dispatch(IEventStore eventStore, IEnumerable<DomainEvent> events)
        {
            _eventDispatchers.ForEach(d => d.Dispatch(eventStore, events));
        }
    }
}
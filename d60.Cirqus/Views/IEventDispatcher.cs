using System.Collections.Generic;
using d60.Cirqus.Events;

namespace d60.Cirqus.Views
{
    /// <summary>
    /// Something that gets to dispatch newly persisted events to something (e.g. to trigger view generation etc)
    /// </summary>
    public interface IEventDispatcher
    {
        /// <summary>
        /// Will be called at startup, before new events will be dispatched. Allows for the dispatcher to 
        /// catch up if it feels like it.
        /// </summary>
        void Initialize(IEventStore eventStore, bool purgeExistingViews = false);

        /// <summary>
        /// Will be called with new events after each unit of work has been successfully committed to the event store
        /// </summary>
        void Dispatch(IEventStore eventStore, IEnumerable<DomainEvent> events);
    }
}
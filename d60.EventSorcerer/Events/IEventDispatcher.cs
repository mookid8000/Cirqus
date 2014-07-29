using System.Collections.Generic;

namespace d60.EventSorcerer.Events
{
    /// <summary>
    /// Something that gets to dispatch newly persisted events to something (e.g. to trigger view generation)
    /// </summary>
    public interface IEventDispatcher
    {
        void Dispatch(IEnumerable<DomainEvent> events);
    }
}
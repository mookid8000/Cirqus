using System.Collections.Generic;

namespace d60.Cirqus.Events
{
    /// <summary>
    /// Something that gets to dispatch newly persisted events to something (e.g. to trigger view generation)
    /// </summary>
    public interface IEventDispatcher
    {
        void Initialize(IEventStore eventStore, bool purgeExistingViews = false);
        void Dispatch(IEventStore eventStore, IEnumerable<DomainEvent> events);
    }
}
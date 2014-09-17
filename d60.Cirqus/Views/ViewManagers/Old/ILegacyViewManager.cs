using System.Collections.Generic;
using d60.Cirqus.Events;

namespace d60.Cirqus.Views.ViewManagers.Old
{
    public interface ILegacyViewManager
    {
        void Initialize(IViewContext context, IEventStore eventStore, bool purgeExistingViews = false);
        bool Stopped { get; set; }
    }

    /// <summary>
    /// Implement this to create a view manager that can catch up, possibly after having been left behind for some time.
    /// Thrown exceptions are handled by the <see cref="LegacyViewManagerEventDispatcher"/>
    /// </summary>
    public interface IPullViewManager : ILegacyViewManager
    {
        void CatchUp(IViewContext context, IEventStore eventStore, long lastGlobalSequenceNumber);
    }

    /// <summary>
    /// Implement this to create a view manager that can have events dispatched directly. Thrown exceptions are handled
    /// by the <see cref="LegacyViewManagerEventDispatcher"/> and the event dispatcher will not dispatch any more events to the view
    /// </summary>
    public interface IPushViewManager : ILegacyViewManager
    {
        void Dispatch(IViewContext context, IEventStore eventStore, IEnumerable<DomainEvent> events);
    }
}
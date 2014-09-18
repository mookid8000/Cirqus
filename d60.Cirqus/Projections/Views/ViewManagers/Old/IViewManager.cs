using System.Collections.Generic;
using d60.Cirqus.Events;

namespace d60.Cirqus.Projections.Views.ViewManagers.Old
{
    public interface IViewManager
    {
        void Initialize(IViewContext context, IEventStore eventStore, bool purgeExistingViews = false);
        bool Stopped { get; set; }
    }

    /// <summary>
    /// Implement this to create a view manager that can catch up, possibly after having been left behind for some time.
    /// Thrown exceptions are handled by the <see cref="ViewManagerEventDispatcher"/>
    /// </summary>
    public interface IPullViewManager : IViewManager
    {
        void CatchUp(IViewContext context, IEventStore eventStore, long lastGlobalSequenceNumber);
    }

    /// <summary>
    /// Implement this to create a view manager that can have events dispatched directly. Thrown exceptions are handled
    /// by the <see cref="ViewManagerEventDispatcher"/> and the event dispatcher will not dispatch any more events to the view
    /// </summary>
    public interface IPushViewManager : IViewManager
    {
        void Dispatch(IViewContext context, IEventStore eventStore, IEnumerable<DomainEvent> events);
    }
}
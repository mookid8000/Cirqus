using System;
using d60.Cirqus.Events;

namespace d60.Cirqus.Views.ViewManagers
{
    /// <summary>
    /// Profiler that can be registered in order to aggregate timing information from views
    /// </summary>
    public interface IViewManagerProfiler
    {
        /// <summary>
        /// Will be called by the view manager for each domain event that is dispatched.
        /// </summary>
        void RegisterTimeSpent(IViewManager viewManager, DomainEvent domainEvent, TimeSpan duration);
    }
}
using System;
using d60.Cirqus.Events;
using d60.Cirqus.Views.ViewManagers;

namespace d60.Cirqus.Views
{
    /// <summary>
    /// Implementation of <see cref="IViewManagerProfiler"/> what does nothing. Allows for not having to deal with null references
    /// all over the place.
    /// </summary>
    public class NullProfiler : IViewManagerProfiler
    {
        public void RegisterTimeSpent(IViewManager viewManager, DomainEvent domainEvent, TimeSpan duration)
        {
        }
    }
}
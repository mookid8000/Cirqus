using System;

namespace d60.Cirqus.Views.ViewManagers
{
    public interface IViewManagerProfiler
    {
        void RegisterTimeSpent(IViewManager viewManager, TimeSpan duration);
    }
}
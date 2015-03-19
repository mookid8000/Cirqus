using System;
using d60.Cirqus.Views.ViewManagers;

namespace d60.Cirqus.Views
{
    class NullProfiler : IViewManagerProfiler
    {
        public void RegisterTimeSpent(IViewManager viewManager, TimeSpan duration)
        {
        }
    }
}
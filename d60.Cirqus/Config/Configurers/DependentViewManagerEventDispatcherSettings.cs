using System;
using System.Collections.Generic;
using d60.Cirqus.Views;
using d60.Cirqus.Views.ViewManagers;

namespace d60.Cirqus.Config.Configurers
{
    /// <summary>
    /// Settings builder for <see cref="DependentViewManagerEventDispatcher"/>
    /// </summary>
    public class DependentViewManagerEventDispatcherSettings
    {
        readonly List<ViewManagerWaitHandle> _waitHandles = new List<ViewManagerWaitHandle>();
        readonly List<IViewManager> _dependentViewManagers = new List<IViewManager>(); 
        readonly Dictionary<string, object> _viewContextItems = new Dictionary<string, object>();

        /// <summary>
        /// Constructs the settings builder with default settings
        /// </summary>
        public DependentViewManagerEventDispatcherSettings()
        {
            MaxDomainEventsPerBatch = 100;
        }

        internal List<ViewManagerWaitHandle> WaitHandles
        {
            get { return _waitHandles; }
        }

        internal List<IViewManager> DependentViewManagers
        {
            get { return _dependentViewManagers; }
        }

        internal Dictionary<string, object> ViewContextItems
        {
            get { return _viewContextItems; }
        }

        /// <summary>
        /// Registers the <see cref="DependentViewManagerEventDispatcher"/> with the given wait handle
        /// </summary>
        public DependentViewManagerEventDispatcherSettings WithWaitHandle(ViewManagerWaitHandle viewManagerWaitHandle)
        {
            if (viewManagerWaitHandle == null) throw new ArgumentNullException("viewManagerWaitHandle");
            _waitHandles.Add(viewManagerWaitHandle);
            return this;
        }

        /// <summary>
        /// Makes the given dictionary of items available in the <see cref="IViewContext"/> passed to the view's
        /// locator and the view itself
        /// </summary>
        public DependentViewManagerEventDispatcherSettings WithViewContext(IDictionary<string, object> viewContextItems)
        {
            if (viewContextItems == null) throw new ArgumentNullException("viewContextItems");
            foreach (var kvp in viewContextItems)
            {
                _viewContextItems[kvp.Key] = kvp.Value;
            }
            return this;
        }

        /// <summary>
        /// Declares a dependency on the given views, which will cause the <see cref="DependentViewManagerEventDispatcher"/> to wait for these
        /// views before delivering events to the views that it manages
        /// </summary>
        public DependentViewManagerEventDispatcherSettings DependentOn(params IViewManager[] dependentViewManagers)
        {
            if (dependentViewManagers == null) throw new ArgumentNullException("dependentViewManagers");
            _dependentViewManagers.AddRange(dependentViewManagers);
            return this;
        }

        /// <summary>
        /// Configures the event dispatcher to persist its state after <paramref name="max"/> events at most
        /// </summary>
        public DependentViewManagerEventDispatcherSettings WithMaxDomainEventsPerBatch(int max)
        {
            MaxDomainEventsPerBatch = max;
            return this;
        }

        internal int MaxDomainEventsPerBatch { get; set; }

        /// <summary>
        /// Registers the given profiler with the event dispatcher, allowing you to aggregate timing information from the view subsystem
        /// </summary>
        public DependentViewManagerEventDispatcherSettings WithProfiler(IViewManagerProfiler profiler)
        {
            if (ViewManagerProfiler != null)
            {
                throw new InvalidOperationException(string.Format("Cannot register view profiler {0} because {1} has already been registered", profiler, ViewManagerProfiler));
            }
            ViewManagerProfiler = profiler;
            return this;
        }

        internal IViewManagerProfiler ViewManagerProfiler { get; set; }
    }
}
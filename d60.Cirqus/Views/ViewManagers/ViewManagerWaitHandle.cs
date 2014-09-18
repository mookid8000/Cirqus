using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace d60.Cirqus.Views.ViewManagers
{
    /// <summary>
    /// The wait handle is used to provide a uniform interface to the ability to wait in a blocking fashion for one or
    /// more specific views to be updated. Construct the handle and pass it into the configuration API when you
    /// configure your view manager event dispatchers
    /// </summary>
    public class ViewManagerWaitHandle
    {
        readonly List<ViewManagerEventDispatcher> _dispatchers = new List<ViewManagerEventDispatcher>();

        internal void Register(ViewManagerEventDispatcher dispatcher)
        {
            _dispatchers.Add(dispatcher);
        }

        /// <summary>
        /// Blocks until the view for the specified view model has processed the events that were emitted in the unit of work
        /// that generated the given <see cref="CommandProcessingResult"/>. If that does not happen before <seealso cref="timeout"/>
        /// has elapsed, a <see cref="TimeoutException"/> is thrown.
        /// </summary>
        public async Task WaitFor<TViewInstance>(CommandProcessingResult result, TimeSpan timeout) where TViewInstance : IViewInstance
        {
            var tasks = _dispatchers
                .Select(d => d.WaitUntilProcessed<TViewInstance>(result, timeout))
                .ToArray();

            await Task.WhenAll(tasks);
        }

        /// <summary>
        /// Blocks until all views have processed the events that were emitted in the unit of work
        /// that generated the given <see cref="CommandProcessingResult"/>. If that does not happen before <seealso cref="timeout"/>
        /// has elapsed, a <see cref="TimeoutException"/> is thrown.
        /// </summary>
        public async Task WaitForAll(CommandProcessingResult result, TimeSpan timeout)
        {
            var tasks = _dispatchers
                .Select(d => d.WaitUntilProcessed(result, timeout))
                .ToArray();

            await Task.WhenAll(tasks);
        }
    }
}
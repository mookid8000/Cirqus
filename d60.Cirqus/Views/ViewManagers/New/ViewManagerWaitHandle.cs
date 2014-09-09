using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace d60.Cirqus.Views.ViewManagers.New
{
    public class ViewManagerWaitHandle
    {
        readonly List<NewViewManagerEventDispatcher> _dispatchers = new List<NewViewManagerEventDispatcher>();

        internal void Register(NewViewManagerEventDispatcher dispatcher)
        {
            _dispatchers.Add(dispatcher);
        }

        public async Task WaitFor<TViewInstance>(CommandProcessingResult result, TimeSpan timeout) where TViewInstance : IViewInstance
        {
            var tasks = _dispatchers
                .Select(d => d.WaitUntilProcessed<TViewInstance>(result, timeout))
                .ToArray();

            await Task.WhenAll(tasks);
        }

        public async Task WaitForAll(CommandProcessingResult result, TimeSpan timeout)
        {
            var tasks = _dispatchers
                .Select(d => d.WaitUntilProcessed(result, timeout))
                .ToArray();

            await Task.WhenAll(tasks);
        }
    }
}
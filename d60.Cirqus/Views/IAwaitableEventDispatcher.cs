using System;
using System.Threading.Tasks;
using d60.Cirqus.Views.ViewManagers;

namespace d60.Cirqus.Views
{
    /// <summary>
    /// Common interface of event dispatchers that can be waited upon
    /// </summary>
    public interface IAwaitableEventDispatcher : IEventDispatcher
    {
        /// <summary>
        /// Waits until the view(s) with the specified view instance type have successfully processed event at least up until those that were emitted
        /// as part of processing the command that yielded the given result
        /// </summary>
        Task WaitUntilProcessed<TViewInstance>(CommandProcessingResult result, TimeSpan timeout) where TViewInstance : IViewInstance;

        /// <summary>
        /// Waits until all view with the specified view instance type have successfully processed event at least up until those that were emitted
        /// as part of processing the command that yielded the given result
        /// </summary>
        Task WaitUntilProcessed(CommandProcessingResult result, TimeSpan timeout);
    }
}
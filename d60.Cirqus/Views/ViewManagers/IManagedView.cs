using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using d60.Cirqus.Events;

namespace d60.Cirqus.Views.ViewManagers
{
    public interface IManagedView
    {
        /// <summary>
        /// Must return the global sequence number that this view knows for sure has been successfully processed
        /// </summary>
        long GetPosition(bool canGetFromCache = true);

        /// <summary>
        /// Must update the view with the specified events
        /// </summary>
        void Dispatch(IViewContext viewContext, IEnumerable<DomainEvent> batch);

        /// <summary>
        /// Must block until the results of the specified command processing result are visible in the view
        /// </summary>
        Task WaitUntilProcessed(CommandProcessingResult result, TimeSpan timeout);

        /// <summary>
        /// Clears all the data in the view - may/may not happen synchronously, but all view data is guaranteed to end up being re-generated
        /// </summary>
        void Purge();
    }

    /// <summary>
    /// Typed API for a managed view that allows for addressing type-specific view managers from the outside of the dispatcher
    /// </summary>
    public interface IManagedView<TViewInstance> : IManagedView where TViewInstance : IViewInstance
    {
        /// <summary>
        /// Loads the view instance with the specified ID, returning null if it does not exist
        /// </summary>
        TViewInstance Load(string viewId);
    }
}
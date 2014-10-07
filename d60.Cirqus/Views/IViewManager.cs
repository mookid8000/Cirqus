using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using d60.Cirqus.Events;
using d60.Cirqus.Views.ViewManagers;

namespace d60.Cirqus.Views
{
    /// <summary>
    /// Defines a projection that can have events dispatched to it while keeping track of how far it has processed
    /// without any errors.
    /// </summary>
    public interface IViewManager
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
}
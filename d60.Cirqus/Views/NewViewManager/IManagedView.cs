using System.Collections.Generic;
using d60.Cirqus.Events;
using d60.Cirqus.Views.ViewManagers;

namespace d60.Cirqus.Views.NewViewManager
{
    public interface IManagedView
    {
        /// <summary>
        /// Must return the lowest global sequence number that this view KNOWS FOR SURE has been successfully processed
        /// </summary>
        long GetLowWatermark();

        /// <summary>
        /// Must update the view
        /// </summary>
        void Dispatch(IViewContext viewContext, IEnumerable<DomainEvent> batch);
    }
}
using System.Collections.Generic;

namespace d60.Cirqus.Views.ViewManagers
{
    /// <summary>
    /// Interface to implement in order to have events of the specified <typeparamref name="TDomainEvent"/> type dispatched to it.
    /// </summary>
    public interface IGetViewIdsFor<TDomainEvent>
    {
        /// <summary>
        /// Gets any number of view IDs that will cause the dispatch of the domain event to the view instances with those IDs
        /// </summary>
        IEnumerable<string> GetViewIds(IViewContext context, TDomainEvent e);
    }
}
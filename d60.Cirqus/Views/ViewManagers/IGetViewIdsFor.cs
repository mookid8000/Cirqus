using System.Collections.Generic;

namespace d60.Cirqus.Views.ViewManagers
{
    public interface IGetViewIdsFor<TDomainEvent>
    {
        IEnumerable<string> GetViewIds(IViewContext context, TDomainEvent e);
    }
}
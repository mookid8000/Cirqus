using d60.Cirqus.NHibernate;
using d60.Cirqus.Views.ViewManagers;

namespace d60.Cirqus.Tests.Contracts.Views.Factories
{
    public class NHibernateViewManagerFactory : AbstractViewManagerFactory
    {
        protected override IViewManager<TViewInstance> CreateViewManager<TViewInstance>()
        {
            return new NHibernateViewManager<TViewInstance>();
        }
    }
}
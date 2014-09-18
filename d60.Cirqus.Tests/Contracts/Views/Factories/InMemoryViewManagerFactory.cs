using d60.Cirqus.Views.ViewManagers;

namespace d60.Cirqus.Tests.Contracts.Views.Factories
{
    public class InMemoryViewManagerFactory : AbstractViewManagerFactory
    {
        protected override IViewManager<TViewInstance> CreateViewManager<TViewInstance>()
        {
            return new InMemoryViewManager<TViewInstance>();
        }
    }
}
using d60.Cirqus.Projections.Views.ViewManagers;

namespace d60.Cirqus.Tests.Contracts.Views.Factories
{
    public class InMemoryManagedViewFactory : AbstractManagedViewFactory
    {
        protected override IManagedView<TViewInstance> CreateManagedView<TViewInstance>()
        {
            return new InMemoryViewManager<TViewInstance>();
        }
    }
}
using d60.Cirqus.Views.ViewManagers.New;

namespace d60.Cirqus.Tests.Contracts.Views.New.Factories
{
    public class MsSqlManagedViewFactory : ManagedViewFactoryBase
    {
        public override IManagedView<TViewInstance> CreateManagedView<TViewInstance>()
        {
            throw new System.NotImplementedException();
        }
    }
}
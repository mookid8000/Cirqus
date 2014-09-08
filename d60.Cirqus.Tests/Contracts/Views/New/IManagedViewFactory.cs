using d60.Cirqus.Views.ViewManagers;
using d60.Cirqus.Views.ViewManagers.New;

namespace d60.Cirqus.Tests.Contracts.Views.New
{
    public interface IManagedViewFactory
    {
        IManagedView<TViewInstance> CreateManagedView<TViewInstance>() where TViewInstance : class, IViewInstance, ISubscribeTo, new();

        TViewInstance Load<TViewInstance>(string viewId) where TViewInstance : class, IViewInstance, ISubscribeTo, new();

        void PurgeView<TViewInstance>() where TViewInstance : class, IViewInstance, ISubscribeTo, new();
    }
}
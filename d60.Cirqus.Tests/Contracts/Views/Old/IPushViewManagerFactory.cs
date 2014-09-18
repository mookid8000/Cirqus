using d60.Cirqus.Projections.Views.ViewManagers;
using d60.Cirqus.Projections.Views.ViewManagers.Old;

namespace d60.Cirqus.Tests.Contracts.Views.Old
{
    public interface IPushViewManagerFactory
    {
        IPushViewManager GetPushViewManager<TView>() where TView : class, IViewInstance, ISubscribeTo, new();

        TView Load<TView>(string viewId) where TView : class, IViewInstance, ISubscribeTo, new();

        void SetMaxDomainEventsBetweenFlush(int value);
    }
}
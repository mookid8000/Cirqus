using d60.Cirqus.Views.ViewManagers;

namespace d60.Cirqus.Tests.Contracts.Views
{
    public interface IPushViewManagerFactory
    {
        IPushViewManager GetPushViewManager<TView>() where TView : class, IViewInstance, ISubscribeTo, new();

        TView Load<TView>(string viewId) where TView : class, IViewInstance, ISubscribeTo, new();

        void SetMaxDomainEventsBetweenFlush(int value);
    }
}
using d60.Circus.Views.ViewManagers;

namespace d60.Circus.Tests.Contracts.Views
{
    public interface IPushViewManagerFactory
    {
        IPushViewManager GetPushViewManager<TView>() where TView : class, IViewInstance, ISubscribeTo, new();

        TView Load<TView>(string viewId) where TView : class, IViewInstance, ISubscribeTo, new();

        void SetMaxDomainEventsBetweenFlush(int value);
    }
}
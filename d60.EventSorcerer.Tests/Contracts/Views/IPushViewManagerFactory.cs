using d60.EventSorcerer.Views.Basic;

namespace d60.EventSorcerer.Tests.Contracts.Views
{
    public interface IPushViewManagerFactory
    {
        IPushViewManager GetPushViewManager<TView>() where TView : class, IViewInstance, ISubscribeTo, new();

        TView Load<TView>(string viewId) where TView : class, IViewInstance, ISubscribeTo, new();

        void SetMaxDomainEventsBetweenFlush(int value);
    }
}
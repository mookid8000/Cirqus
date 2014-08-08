using d60.EventSorcerer.Views.Basic;

namespace d60.EventSorcerer.Tests.Contracts.Views
{
    public interface IViewManagerFactory
    {
        IViewManager GetViewManagerFor<TView>() where TView : class, IViewInstance, ISubscribeTo, new();

        TView Load<TView>(string id) where TView : class, IViewInstance, ISubscribeTo, new();
        void SetMaxDomainEventsBetweenFlush(int value);
    }
}
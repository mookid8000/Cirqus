using d60.EventSorcerer.Views.Basic;

namespace d60.EventSorcerer.Tests.Contracts.Views.Factories
{
    public class EntityFrameworkViewManagerFactory : IViewManagerFactory
    {
        public IViewManager GetViewManagerFor<TView>() where TView : class, IView, ISubscribeTo, new()
        {
            throw new System.NotImplementedException();
        }

        public TView Load<TView>(string id) where TView : class, IView, ISubscribeTo, new()
        {
            throw new System.NotImplementedException();
        }

        public void SetMaxDomainEventsBetweenFlush(int value)
        {
            throw new System.NotImplementedException();
        }
    }
}
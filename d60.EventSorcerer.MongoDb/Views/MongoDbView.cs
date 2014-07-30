using d60.EventSorcerer.Events;
using d60.EventSorcerer.Views.Basic;

namespace d60.EventSorcerer.MongoDb.Views
{
    class MongoDbView<TView> where TView : IView, ISubscribeTo
    {
        protected readonly ViewDispatcherHelper<TView> Dispatcher = new ViewDispatcherHelper<TView>();

        public string Id { get; set; }

        public TView View { get; set; }

        public void Dispatch(DomainEvent domainEvent)
        {
            Dispatcher.DispatchToView(domainEvent, View);
        }
    }
}
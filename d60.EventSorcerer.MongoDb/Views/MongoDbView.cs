using System.Collections.Generic;
using d60.EventSorcerer.Events;
using d60.EventSorcerer.Views.Basic;

namespace d60.EventSorcerer.MongoDb.Views
{
    class MongoDbView<TView> where TView : IView, ISubscribeTo
    {
        protected readonly ViewDispatcherHelper<TView> Dispatcher = new ViewDispatcherHelper<TView>();

        public string Id { get; set; }

        public TView View { get; set; }

        public virtual void Dispatch(DomainEvent domainEvent)
        {
            Dispatcher.DispatchToView(domainEvent, View);
        }
    }

    class MongoDbCatchUpView<TView> : MongoDbView<TView> where TView : IView, ISubscribeTo
    {
        public MongoDbCatchUpView()
        {
            Pointers = new Dictionary<string, int>();
        }
        public Dictionary<string, int> Pointers { get; set; }
        public override void Dispatch(DomainEvent domainEvent)
        {
            base.Dispatch(domainEvent);
        }
    }
}
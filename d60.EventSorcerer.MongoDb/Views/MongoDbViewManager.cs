using System.Collections;
using System.Collections.Generic;
using System.Linq;
using d60.EventSorcerer.Events;
using d60.EventSorcerer.Views.Basic;
using MongoDB.Driver;

namespace d60.EventSorcerer.MongoDb.Views
{
    public class MongoDbViewManager<TView> : IEnumerable<TView>, IViewManager
        where TView : class, IView, ISubscribeTo, new()
    {
        readonly ViewDispatcherHelper<TView> _viewDispatcherHelper = new ViewDispatcherHelper<TView>();
        readonly MongoCollection<MongoDbView<TView>> _viewCollection;

        public MongoDbViewManager(MongoDatabase database, string collectionName)
        {
            _viewCollection = database.GetCollection<MongoDbView<TView>>(collectionName);
        }

        public TView Load(string viewId)
        {
            var doc = _viewCollection.FindOneById(viewId);

            return doc != null
                ? doc.View
                : null;
        }

        public IEnumerator<TView> GetEnumerator()
        {
            return _viewCollection.FindAll()
                .Select(d => d.View)
                .GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Dispatch(IEventStore eventStore, IEnumerable<DomainEvent> events)
        {
            foreach (var e in events)
            {
                var viewId = ViewLocator.GetLocatorFor<TView>().GetViewId(e);

                var doc = _viewCollection.FindOneById(viewId)
                           ?? new MongoDbView<TView>
                           {
                               Id = viewId,
                               View = new TView()
                           };

                _viewDispatcherHelper.DispatchToView(e, doc.View);

                _viewCollection.Save(doc);
            }
        }
    }

    class MongoDbView<TView> where TView : IView
    {
        public string Id { get; set; }

        public TView View { get; set; }
    }
}
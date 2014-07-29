using System.Collections;
using System.Collections.Generic;
using d60.EventSorcerer.Events;
using d60.EventSorcerer.Views.Basic;
using MongoDB.Driver;

namespace d60.EventSorcerer.MongoDb.Views
{
    public class MongoDbViewManager<TView> : IEnumerable<TView>, IViewManager
        where TView : class, IMongoDbView, new()
    {
        readonly ViewDispatcherHelper<TView> _viewDispatcherHelper = new ViewDispatcherHelper<TView>();
        readonly MongoCollection<TView> _viewCollection;

        public MongoDbViewManager(MongoCollection<TView> viewCollection)
        {
            _viewCollection = viewCollection;
        }

        public IEnumerator<TView> GetEnumerator()
        {
            return _viewCollection.FindAll().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Dispatch(IEnumerable<DomainEvent> events)
        {
            foreach (var e in events)
            {
                var viewId = ViewLocator.GetLocatorFor<TView>().GetViewId(e);

                var view = _viewCollection.FindOneById(viewId)
                           ?? new TView();

                view.Id = viewId;

                _viewDispatcherHelper.DispatchToView(e, view);

                _viewCollection.Save(view);
            }
        }
    }
}
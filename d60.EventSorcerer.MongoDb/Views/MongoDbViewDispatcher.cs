using System.Collections;
using System.Collections.Generic;
using d60.EventSorcerer.Events;
using d60.EventSorcerer.Views.Basic;
using MongoDB.Driver;

namespace d60.EventSorcerer.MongoDb.Views
{
    public class MongoDbViewDispatcher<TView> : IEnumerable<TView>, IViewDispatcher
        where TView : class, IMongoDbView, new()
    {
        readonly ViewDispatcher<TView> _viewDispatcher = new ViewDispatcher<TView>();
        readonly MongoCollection<TView> _viewCollection;

        public MongoDbViewDispatcher(MongoCollection<TView> viewCollection)
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

                _viewDispatcher.DispatchToView(e, view);

                _viewCollection.Save(view);
            }
        }
    }
}
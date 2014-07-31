using System.Collections.Generic;
using d60.EventSorcerer.Events;
using d60.EventSorcerer.Extensions;
using d60.EventSorcerer.Views.Basic;
using MongoDB.Driver;

namespace d60.EventSorcerer.MongoDb.Views
{
    public class MongoDbCatchUpViewManager<TView> : IViewManager where TView : class, IView, ISubscribeTo, new()
    {
        readonly MongoCollection<MongoDbCatchUpView<TView>> _viewCollection;

        public MongoDbCatchUpViewManager(MongoDatabase database, string collectionName)
        {
            _viewCollection = database.GetCollection<MongoDbCatchUpView<TView>>(collectionName);
        }

        public void Initialize(IEventStore eventStore)
        {
            
        }

        public void Dispatch(IEventStore eventStore, IEnumerable<DomainEvent> events)
        {
            var locator = ViewLocator.GetLocatorFor<TView>();
            var activeViewDocs = new Dictionary<string, MongoDbCatchUpView<TView>>();

            foreach (var e in events)
            {
                var viewId = locator.GetViewId(e);
                var doc = activeViewDocs
                    .GetOrAdd(viewId, id => _viewCollection.FindOneById(id)
                                            ?? new MongoDbCatchUpView<TView>
                                            {
                                                Id = id,
                                                View = new TView(),
                                            });

                doc.DispatchAndResolve(eventStore, e);
            }

            foreach (var doc in activeViewDocs.Values)
            {
                _viewCollection.Save(doc);
            }
        }

        public TView Load(string viewId)
        {
            var doc = _viewCollection.FindOneById(viewId);

            return doc != null
                ? doc.View
                : null;
        }
    }
}
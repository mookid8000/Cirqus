using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using d60.Cirqus.Events;
using d60.Cirqus.Extensions;
using d60.Cirqus.Views.ViewManagers;
using d60.Cirqus.Views.ViewManagers.Old;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using MongoDB.Driver.Linq;

namespace d60.Cirqus.MongoDb.Views.Old
{
    public class MongoDbViewManager<TView> : IPushViewManager, IPullViewManager where TView : class, IViewInstance, ISubscribeTo, new()
    {
        readonly MongoCollection<TView> _viewCollection;
        readonly ViewDispatcherHelper<TView> _dispatcherHelper = new ViewDispatcherHelper<TView>();
        bool _initialized;
        int _maxDomainEventsBetweenFlush = 100;
        long _lastGlobalSequenceNumberProcessed = -1;

        public MongoDbViewManager(MongoDatabase database, string collectionName)
        {
            _viewCollection = database.GetCollection<TView>(collectionName);
            _viewCollection.CreateIndex(IndexKeys<TView>.Ascending(v => v.LastGlobalSequenceNumber));
        }

        public MongoDbViewManager(MongoCollection<TView> viewCollection)
        {
            _viewCollection = viewCollection;
            _viewCollection.CreateIndex(IndexKeys<TView>.Ascending(v => v.LastGlobalSequenceNumber));
        }

        public IQueryable<TView> Linq()
        {
            return _viewCollection
                .AsQueryable();
        }

        public void CreateIndex(Expression<Func<TView, object>> expression, bool ascending = true)
        {
            var indexKeys = ascending
                ? IndexKeys<TView>.Ascending(expression)
                : IndexKeys<TView>.Descending(expression);

            _viewCollection.CreateIndex(indexKeys);
        }

        /// <summary>
        /// Configures how many events are dispatched to view instances between
        /// </summary>
        public int MaxDomainEventsBetweenFlush
        {
            get { return _maxDomainEventsBetweenFlush; }
            set
            {
                if (value <= 0)
                {
                    throw new ArgumentException(string.Format("Attempted to set MaxDomainEventsBetweenFlush to {0}, but it must be greater than 0!", value));
                }
                _maxDomainEventsBetweenFlush = value;
            }
        }

        public void Initialize(IViewContext context, IEventStore eventStore, bool purgeExistingViews = false)
        {
            if (purgeExistingViews)
            {
                Purge();
            }

            // catch up with no limits :)
            CatchUp(context, eventStore, long.MaxValue);

            _initialized = true;
        }

        public void CatchUp(IViewContext context, IEventStore eventStore, long lastGlobalSequenceNumber)
        {
            if (lastGlobalSequenceNumber <= _lastGlobalSequenceNumberProcessed) return;

            var viewInstanceWithMaxGlobalSequenceNumber = _viewCollection
                .FindAllAs<TView>()
                .SetSortOrder(SortBy<TView>.Descending(v => v.LastGlobalSequenceNumber))
                .SetLimit(1)
                .FirstOrDefault();

            var globalSequenceNumberCutoff = viewInstanceWithMaxGlobalSequenceNumber == null
                ? 0
                : viewInstanceWithMaxGlobalSequenceNumber.LastGlobalSequenceNumber + 1;

            var batches = eventStore.Stream(globalSequenceNumberCutoff).Batch(1000);

            foreach (var batch in batches)
            {
                InnerDispatch(context, batch);
            }
        }

        public void Dispatch(IViewContext context, IEventStore eventStore, IEnumerable<DomainEvent> events)
        {
            if (!_initialized)
            {
                var message =
                    string.Format("The view manager for {0} has not been initialized! Please make sure that the view" +
                                  " manager is properly initialized, either by initializing it manually, or by having" +
                                  " the event dispatcher do it (which is the preferred way when you\'re working with" +
                                  " an event dispatcher)",
                        typeof(TView));

                throw new InvalidOperationException(message);
            }

            InnerDispatch(context, events);
        }

        void InnerDispatch(IViewContext context, IEnumerable<DomainEvent> events)
        {
            var eventsList = events.ToList();

            try
            {
                foreach (var batch in eventsList.Batch(MaxDomainEventsBetweenFlush))
                {
                    ProcessOneBatch(batch, context);
                }
            }
            catch (Exception)
            {
                foreach (var batch in eventsList.Batch(1))
                {
                    ProcessOneBatch(batch, context);
                }

                throw;
            }
        }

        public void Purge()
        {
            _viewCollection.Drop();
        }

        public bool Stopped { get; set; }

        void ProcessOneBatch(IEnumerable<DomainEvent> batch, IViewContext context)
        {
            var locator = ViewLocator.GetLocatorFor<TView>();
            var activeViewDocsByid = new Dictionary<string, TView>();

            foreach (var e in batch)
            {
                if (!ViewLocator.IsRelevant<TView>(e)) continue;

                var globalSequenceNumberOfThisEvent = e.GetGlobalSequenceNumber();

                if (globalSequenceNumberOfThisEvent <= _lastGlobalSequenceNumberProcessed) continue;

                var viewIds = locator.GetViewIds(e);

                foreach (var viewId in viewIds)
                {
                    var doc = activeViewDocsByid
                        .GetOrAdd(viewId, id => _viewCollection.FindOneById(id)
                                                ?? new TView
                                                {
                                                    Id = id,
                                                    LastGlobalSequenceNumber = -1
                                                });

                    _dispatcherHelper.DispatchToView(context, e, doc);
                }
            }

            Save(activeViewDocsByid.Values);
        }

        void Save(IEnumerable<TView> activeViews)
        {
            foreach (var view in activeViews.OrderBy(v => v.LastGlobalSequenceNumber))
            {
                _viewCollection.Save(view);

                _lastGlobalSequenceNumberProcessed = view.LastGlobalSequenceNumber;
            }
        }

        public TView Load(string viewId)
        {
            var view = _viewCollection.FindOneById(viewId);

            return view;
        }
    }
}
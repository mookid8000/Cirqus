using System;
using System.Collections.Generic;
using System.Linq;
using d60.EventSorcerer.Events;
using d60.EventSorcerer.Extensions;
using d60.EventSorcerer.Views.Basic;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using MongoDB.Driver.Linq;

namespace d60.EventSorcerer.MongoDb.Views
{
    public class MongoDbCatchUpViewManager<TView> : IDirectDispatchViewManager, ICatchUpViewManager where TView : class, IView, ISubscribeTo, new()
    {
        readonly MongoCollection<TView> _viewCollection;
        readonly ViewDispatcherHelper<TView> _dispatcherHelper = new ViewDispatcherHelper<TView>();
        int _maxDomainEventsBetweenFlush = 100;
        long _lastGlobalSequenceNumberProcessed = -1;

        public MongoDbCatchUpViewManager(MongoDatabase database, string collectionName)
        {
            _viewCollection = database.GetCollection<TView>(collectionName);
            _viewCollection.CreateIndex(IndexKeys<TView>.Ascending(v => v.LastGlobalSequenceNumber));
        }

        public IQueryable<TView> Linq()
        {
            return _viewCollection
                .AsQueryable();
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
                Dispatch(context, eventStore, batch);
            }
        }

        public void Dispatch(IViewContext context, IEventStore eventStore, IEnumerable<DomainEvent> events)
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

                var viewId = locator.GetViewId(e);
                var doc = activeViewDocsByid
                    .GetOrAdd(viewId, id => _viewCollection.FindOneById(id)
                                            ?? new TView { Id = id });

                _dispatcherHelper.DispatchToView(context, e, doc);

                _lastGlobalSequenceNumberProcessed = globalSequenceNumberOfThisEvent;
            }

            Save(activeViewDocsByid.Values);
        }

        void Save(IEnumerable<TView> activeViews)
        {
            foreach (var view in activeViews)
            {
                _viewCollection.Save(view);
            }
        }

        public TView Load(string viewId)
        {
            var view = _viewCollection.FindOneById(viewId);

            return view;
        }
    }
}
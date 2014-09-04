using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using d60.Cirqus.Events;
using d60.Cirqus.Extensions;
using d60.Cirqus.Logging;
using d60.Cirqus.Views.NewViewManager;
using d60.Cirqus.Views.ViewManagers;
using d60.Cirqus.Views.ViewManagers.Locators;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;

namespace d60.Cirqus.MongoDb.Views
{
    public class NewMongoDbViewManager<TViewInstance> : IManagedView<TViewInstance> where TViewInstance : class, IViewInstance, ISubscribeTo, new()
    {
        const string LowWatermarkId = "__low_watermark__";
        const string LowWatermarkPropertyName = "LastGlobalSequenceNumber";

        readonly ViewDispatcherHelper<TViewInstance> _dispatcherHelper = new ViewDispatcherHelper<TViewInstance>();
        readonly MongoCollection<TViewInstance> _viewCollection;
        readonly ViewLocator _viewLocator;

        static Logger _logger;

        static NewMongoDbViewManager()
        {
            CirqusLoggerFactory.Changed += f => _logger = f.GetCurrentClassLogger();
        }

        long? _cachedLowWatermark;

        public NewMongoDbViewManager(MongoDatabase database)
            : this(database, typeof(TViewInstance).Name)
        {
        }

        public NewMongoDbViewManager(MongoDatabase database, string collectionName)
        {
            try
            {
                _viewLocator = ViewLocator.GetLocatorFor<TViewInstance>();
            }
            catch (Exception exception)
            {
                var message =
                    string.Format("Could not successfully retrieve the view locator for the type {0} - please make" +
                                  " sure that your view class implements IViewInstance<T> where T is one of the" +
                                  " available view locators (e.g. {1} or {2} or a custom locator)",
                        typeof(TViewInstance), typeof(InstancePerAggregateRootLocator).Name,
                        typeof(GlobalInstanceLocator).Name);

                throw new ArgumentException(message, exception);
            }

            _viewCollection = database.GetCollection<TViewInstance>(collectionName);
            _viewCollection.CreateIndex(IndexKeys<TViewInstance>.Ascending(i => i.LastGlobalSequenceNumber));
        }

        public TViewInstance Load(string viewId)
        {
            return _viewCollection.FindOneById(viewId);
        }

        public long GetLowWatermark(bool canGetFromCache = true)
        {
            if (canGetFromCache)
            {
                return GetLowWatermarkFromMemory()
                       ?? GetLowWatermarkFromPersistentCache()
                       ?? GetLowWatermarkFromViewInstances()
                       ?? -1;
            }

            return GetLowWatermarkFromPersistentCache()
                       ?? GetLowWatermarkFromViewInstances()
                       ?? -1;
        }

        public void Purge()
        {
            _viewCollection.RemoveAll();
            _cachedLowWatermark = null;
        }

        public async Task WaitUntilDispatched(CommandProcessingResult result)
        {
            if (!result.EventsWereEmitted) return;

            var mostRecentGlobalSequenceNumber = result.GlobalSequenceNumbersOfEmittedEvents.Max();

            while (GetLowWatermark() < mostRecentGlobalSequenceNumber)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(10));
            }
        }

        void UpdateLowWatermark(long newLowWatermark)
        {
            _viewCollection.Update(Query.EQ("_id", LowWatermarkId),
                Update.Set(LowWatermarkPropertyName, newLowWatermark),
                UpdateFlags.Upsert);

            _cachedLowWatermark = newLowWatermark;
        }

        public void Dispatch(IViewContext viewContext, IEnumerable<DomainEvent> batch)
        {
            var cachedViewInstances = new Dictionary<string, TViewInstance>();

            var eventList = batch.ToList();

            if (!eventList.Any()) return;

            foreach (var e in eventList)
            {
                if (!ViewLocator.IsRelevant<TViewInstance>(e)) continue;

                var viewId = _viewLocator.GetViewId(e);
                if (viewId == null) continue;

                var viewInstance = GetOrCreateViewInstance(viewId, cachedViewInstances);

                _dispatcherHelper.DispatchToView(viewContext, e, viewInstance);
            }

            FlushCacheToDatabase(cachedViewInstances);

            UpdateLowWatermark(eventList.Max(e => e.GetGlobalSequenceNumber()));
        }

        void FlushCacheToDatabase(Dictionary<string, TViewInstance> cachedViewInstances)
        {
            if (!cachedViewInstances.Any()) return;

            foreach (var viewInstance in cachedViewInstances.Values)
            {
                _viewCollection.Save(viewInstance);
            }
        }

        TViewInstance GetOrCreateViewInstance(string viewId, Dictionary<string, TViewInstance> cachedViewInstances)
        {
            TViewInstance instanceToReturn;

            if (cachedViewInstances.TryGetValue(viewId, out instanceToReturn))
                return instanceToReturn;

            instanceToReturn = _viewCollection.FindOneById(viewId)
                               ?? new TViewInstance { Id = viewId };

            cachedViewInstances[viewId] = instanceToReturn;

            return instanceToReturn;
        }

        long? GetLowWatermarkFromMemory()
        {
            return _cachedLowWatermark;
        }

        long? GetLowWatermarkFromPersistentCache()
        {
            var lowWatermarkDocument = _viewCollection
                .FindOneByIdAs<BsonDocument>(LowWatermarkId);

            if (lowWatermarkDocument != null)
            {
                return lowWatermarkDocument[LowWatermarkPropertyName].AsInt64;
            }

            return null;
        }

        long? GetLowWatermarkFromViewInstances()
        {
            var viewWithTheLowestGlobalSequenceNumber =
                _viewCollection
                    .FindAll()
                    .SetFields(Fields<TViewInstance>.Include(i => i.LastGlobalSequenceNumber).Exclude(i => i.Id))
                    .SetLimit(1)
                    .SetSortOrder(SortBy<TViewInstance>.Ascending(v => v.LastGlobalSequenceNumber))
                    .FirstOrDefault();

            return viewWithTheLowestGlobalSequenceNumber != null
                ? viewWithTheLowestGlobalSequenceNumber.LastGlobalSequenceNumber
                : default(long?);
        }
    }
}
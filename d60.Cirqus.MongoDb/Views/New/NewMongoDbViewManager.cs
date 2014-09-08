using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using d60.Cirqus.Events;
using d60.Cirqus.Extensions;
using d60.Cirqus.Logging;
using d60.Cirqus.Numbers;
using d60.Cirqus.Views.ViewManagers;
using d60.Cirqus.Views.ViewManagers.Locators;
using d60.Cirqus.Views.ViewManagers.New;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;

namespace d60.Cirqus.MongoDb.Views.New
{
    public class NewMongoDbViewManager<TViewInstance> : IManagedView<TViewInstance> where TViewInstance : class, IViewInstance, ISubscribeTo, new()
    {
        const string LowWatermarkDocId = "__low_watermark__";
        const string LowWatermarkPropertyName = "LastGlobalSequenceNumber";
        const int DefaultLowWatermark = -1;

        readonly ViewDispatcherHelper<TViewInstance> _dispatcherHelper = new ViewDispatcherHelper<TViewInstance>();
        readonly MongoCollection<TViewInstance> _viewCollection;
        readonly ViewLocator _viewLocator;

        static Logger _logger;

        static NewMongoDbViewManager()
        {
            CirqusLoggerFactory.Changed += f => _logger = f.GetCurrentClassLogger();
        }

        long _cachedLowWatermark;

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
                       ?? GetDefaultLowWatermark();
            }

            return GetLowWatermarkFromPersistentCache()
                       ?? GetLowWatermarkFromViewInstances()
                       ?? GetDefaultLowWatermark();
        }

        static int GetDefaultLowWatermark()
        {
            return DefaultLowWatermark;
        }

        public void Purge()
        {
            _logger.Info("Purging MongoDB collection {0}", _viewCollection.Name);

            _viewCollection.RemoveAll();

            Interlocked.Exchange(ref _cachedLowWatermark, DefaultLowWatermark);
        }

        public async Task WaitUntilDispatched(CommandProcessingResult result, TimeSpan timeout)
        {
            if (!result.EventsWereEmitted) return;

            var mostRecentGlobalSequenceNumber = result.GlobalSequenceNumbersOfEmittedEvents.Max();

            var waitStartTime = DateTime.UtcNow;

            while (GetLowWatermark(canGetFromCache: false) < mostRecentGlobalSequenceNumber)
            {
                if (DateTime.UtcNow - waitStartTime > timeout)
                {
                    throw new TimeoutException(string.Format("View for {0} did not catch up to {1} within {2} timeout!",
                        typeof(TViewInstance), mostRecentGlobalSequenceNumber, timeout));
                }

                await Task.Delay(TimeSpan.FromMilliseconds(10));
            }
        }

        void UpdateLowWatermark(long newLowWatermark)
        {
            _viewCollection.Update(Query.EQ("_id", LowWatermarkDocId),
                Update.Set(LowWatermarkPropertyName, newLowWatermark),
                UpdateFlags.Upsert);

            Interlocked.Exchange(ref _cachedLowWatermark, newLowWatermark);
        }

        public void Dispatch(IViewContext viewContext, IEnumerable<DomainEvent> batch)
        {
            var cachedViewInstances = new Dictionary<string, TViewInstance>();

            var eventList = batch.ToList();

            if (!eventList.Any()) return;

            foreach (var e in eventList)
            {
                if (!ViewLocator.IsRelevant<TViewInstance>(e)) continue;

                var viewIds = _viewLocator.GetViewIds(e);

                foreach (var viewId in viewIds)
                {
                    var viewInstance = GetOrCreateViewInstance(viewId, cachedViewInstances);

                    _dispatcherHelper.DispatchToView(viewContext, e, viewInstance);
                }
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
                               ?? _dispatcherHelper.CreateNewInstance(viewId);

            cachedViewInstances[viewId] = instanceToReturn;

            return instanceToReturn;
        }

        long? GetLowWatermarkFromMemory()
        {
            var value = Interlocked.Read(ref _cachedLowWatermark);

            return value != DefaultLowWatermark ? value : default(long?);
        }

        long? GetLowWatermarkFromPersistentCache()
        {
            var lowWatermarkDocument = _viewCollection
                .FindOneByIdAs<BsonDocument>(LowWatermarkDocId);

            if (lowWatermarkDocument != null)
            {
                var lowWatermark = lowWatermarkDocument[LowWatermarkPropertyName].AsInt64;

                return lowWatermark;
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

            if (viewWithTheLowestGlobalSequenceNumber != null)
            {
                var lowWatermark = viewWithTheLowestGlobalSequenceNumber.LastGlobalSequenceNumber;

                return lowWatermark;
            }

            return default(long?);
        }
    }
}
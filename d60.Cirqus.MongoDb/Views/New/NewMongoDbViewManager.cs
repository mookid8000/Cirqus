using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using d60.Cirqus.Events;
using d60.Cirqus.Extensions;
using d60.Cirqus.Logging;
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
        const long DefaultLowWatermark = -1;

        readonly ViewDispatcherHelper<TViewInstance> _dispatcherHelper = new ViewDispatcherHelper<TViewInstance>();
        readonly MongoCollection<TViewInstance> _viewCollection;
        readonly ViewLocator _viewLocator;

        static Logger _logger;

        static NewMongoDbViewManager()
        {
            CirqusLoggerFactory.Changed += f => _logger = f.GetCurrentClassLogger();
        }

        long _cachedLowWatermark;

        public NewMongoDbViewManager(string mongoDbConnectionString)
            : this(GetDatabaseFromConnectionString(mongoDbConnectionString))
        {
        }

        public NewMongoDbViewManager(string mongoDbConnectionString, string collectionName)
            : this(GetDatabaseFromConnectionString(mongoDbConnectionString), collectionName)
        {
        }

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

            _logger.Info("Create index in '{0}': '{1}'", collectionName, LowWatermarkPropertyName);
            _viewCollection.CreateIndex(IndexKeys<TViewInstance>.Ascending(i => i.LastGlobalSequenceNumber), IndexOptions.SetName(LowWatermarkPropertyName));
        }

        public TViewInstance Load(string viewId)
        {
            return _viewCollection.FindOneById(viewId);
        }

        public long GetLowWatermark(bool canGetFromCache = true)
        {
            if (canGetFromCache && false)
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

        static long GetDefaultLowWatermark()
        {
            return DefaultLowWatermark;
        }

        public void Purge()
        {
            _logger.Info("Purging '{0}'", _viewCollection.Name);

            _viewCollection.RemoveAll();

            Interlocked.Exchange(ref _cachedLowWatermark, DefaultLowWatermark);
        }

        public async Task WaitUntilProcessed(CommandProcessingResult result, TimeSpan timeout)
        {
            if (!result.EventsWereEmitted) return;

            var mostRecentGlobalSequenceNumber = result.GlobalSequenceNumbersOfEmittedEvents.Max();

            var stopwatch = Stopwatch.StartNew();

            while (GetLowWatermark(canGetFromCache: false) < mostRecentGlobalSequenceNumber)
            {
                if (stopwatch.Elapsed > timeout)
                {
                    throw new TimeoutException(string.Format("View for {0} did not catch up to {1} within {2} timeout!",
                        typeof(TViewInstance), mostRecentGlobalSequenceNumber, timeout));
                }

                await Task.Delay(TimeSpan.FromMilliseconds(10));
            }
        }

        void UpdateLowWatermark(long newLowWatermark)
        {
            _logger.Debug("Updating persistent low watermark to {0}", newLowWatermark);

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

                var viewIds = _viewLocator.GetAffectedViewIds(viewContext, e);

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

            _logger.Debug("Flushing {0} view instances to '{1}'", cachedViewInstances.Values.Count, _viewCollection.Name);

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

            if (value != DefaultLowWatermark)
                return value;

            return null;
        }

        long? GetLowWatermarkFromPersistentCache()
        {
            var lowWatermarkDocument = _viewCollection
                .FindOneByIdAs<BsonDocument>(LowWatermarkDocId);

            if (lowWatermarkDocument == null) return null;
            
            var lowWatermark = lowWatermarkDocument[LowWatermarkPropertyName].AsInt64;

            return lowWatermark;
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

            if (viewWithTheLowestGlobalSequenceNumber == null) return null;
            
            var lowWatermark = viewWithTheLowestGlobalSequenceNumber.LastGlobalSequenceNumber;

            return lowWatermark;
        }

        static MongoDatabase GetDatabaseFromConnectionString(string mongoDbConnectionString)
        {
            var mongoUrl = new MongoUrl(mongoDbConnectionString);

            if (string.IsNullOrWhiteSpace(mongoUrl.DatabaseName))
            {
                throw new ConfigurationErrorsException(string.Format("MongoDB URL does not contain a database name!: {0}", mongoDbConnectionString));
            }

            return new MongoClient(mongoUrl)
                .GetServer()
                .GetDatabase(mongoUrl.DatabaseName);
        }
    }
}
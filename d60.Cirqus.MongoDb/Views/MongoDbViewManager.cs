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
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;

namespace d60.Cirqus.MongoDb.Views
{
    public class MongoDbViewManager<TViewInstance> : IManagedView<TViewInstance> where TViewInstance : class, IViewInstance, ISubscribeTo, new()
    {
        const string CurrentPositionDocId = "__current_position__";
        const string CurrentPositionPropertyName = "LastGlobalSequenceNumber";
        const long DefaultPosition = -1;

        readonly ViewDispatcherHelper<TViewInstance> _dispatcherHelper = new ViewDispatcherHelper<TViewInstance>();
        readonly MongoCollection<TViewInstance> _viewCollection;
        readonly ViewLocator _viewLocator;

        Logger _logger;

        long _cachedPosition;

        public MongoDbViewManager(MongoDatabase database, string collectionName)
        {
            CirqusLoggerFactory.Changed += f => _logger = f.GetCurrentClassLogger();

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

            _logger.Info("Create index in '{0}': '{1}'", collectionName, CurrentPositionPropertyName);
            _viewCollection.CreateIndex(IndexKeys<TViewInstance>.Ascending(i => i.LastGlobalSequenceNumber), IndexOptions.SetName(CurrentPositionPropertyName));
        }

        public MongoDbViewManager(string mongoDbConnectionString)
            : this(GetDatabaseFromConnectionString(mongoDbConnectionString))
        {
        }

        public MongoDbViewManager(string mongoDbConnectionString, string collectionName)
            : this(GetDatabaseFromConnectionString(mongoDbConnectionString), collectionName)
        {
        }

        public MongoDbViewManager(MongoDatabase database)
            : this(database, typeof(TViewInstance).Name)
        {
        }

        public TViewInstance Load(string viewId)
        {
            return _viewCollection.FindOneById(viewId);
        }

        public long GetPosition(bool canGetFromCache = true)
        {
            if (canGetFromCache && false)
            {
                return GetPositionFromMemory()
                       ?? GetPositionFromPersistentCache()
                       ?? GetPositionFromViewInstances()
                       ?? GetDefaultPosition();
            }

            return GetPositionFromPersistentCache()
                       ?? GetPositionFromViewInstances()
                       ?? GetDefaultPosition();
        }

        static long GetDefaultPosition()
        {
            return DefaultPosition;
        }

        public void Purge()
        {
            _logger.Info("Purging '{0}'", _viewCollection.Name);

            _viewCollection.RemoveAll();

            Interlocked.Exchange(ref _cachedPosition, DefaultPosition);
        }

        public async Task WaitUntilProcessed(CommandProcessingResult result, TimeSpan timeout)
        {
            if (!result.EventsWereEmitted) return;

            var mostRecentGlobalSequenceNumber = result.GetNewPosition();

            var stopwatch = Stopwatch.StartNew();

            while (GetPosition(canGetFromCache: false) < mostRecentGlobalSequenceNumber)
            {
                if (stopwatch.Elapsed > timeout)
                {
                    throw new TimeoutException(string.Format("View for {0} did not catch up to {1} within {2} timeout!",
                        typeof(TViewInstance), mostRecentGlobalSequenceNumber, timeout));
                }

                await Task.Delay(TimeSpan.FromMilliseconds(10));
            }
        }

        void UpdatePersistentCache(long newPosition)
        {
            _logger.Debug("Updating persistent position cache to {0}", newPosition);

            _viewCollection.Update(Query.EQ("_id", CurrentPositionDocId),
                Update.Set(CurrentPositionPropertyName, newPosition),
                UpdateFlags.Upsert);

            Interlocked.Exchange(ref _cachedPosition, newPosition);
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

            UpdatePersistentCache(eventList.Max(e => e.GetGlobalSequenceNumber()));
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

        long? GetPositionFromMemory()
        {
            var value = Interlocked.Read(ref _cachedPosition);

            if (value != DefaultPosition)
                return value;

            return null;
        }

        long? GetPositionFromPersistentCache()
        {
            var currentPositionDocument = _viewCollection
                .FindOneByIdAs<BsonDocument>(CurrentPositionDocId);

            if (currentPositionDocument == null) return null;
            
            var currentPosition = currentPositionDocument[CurrentPositionPropertyName].AsInt64;

            return currentPosition;
        }

        long? GetPositionFromViewInstances()
        {
            // with MongoDB, we cannot know for sure how many events we've successfully processed of those that
            // have sequence numbers between the MIN and MAX sequence numbers currently stored in our views
            // - therefore, to be safe, we need to pick the MIN as our starting point....

            var onlyTheSequenceNumber = Fields<TViewInstance>.Include(i => i.LastGlobalSequenceNumber).Exclude(i => i.Id);
            
            var ascendingBySequenceNumber = SortBy<TViewInstance>.Ascending(v => v.LastGlobalSequenceNumber);

            var viewWithTheLowestGlobalSequenceNumber =
                _viewCollection
                    .FindAll()
                    .SetFields(onlyTheSequenceNumber)
                    .SetLimit(1)
                    .SetSortOrder(ascendingBySequenceNumber)
                    .FirstOrDefault();

            if (viewWithTheLowestGlobalSequenceNumber == null) return null;
            
            var lowPosition = viewWithTheLowestGlobalSequenceNumber.LastGlobalSequenceNumber;

            return lowPosition;
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
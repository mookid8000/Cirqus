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
using MongoDB.Driver;
using MongoDB.Driver.Builders;

namespace d60.Cirqus.MongoDb.Views
{
    public class MongoDbViewManager<TViewInstance> : AbstractViewManager<TViewInstance> where TViewInstance : class, IViewInstance, ISubscribeTo, new()
    {
        const string CurrentPositionPropertyName = "LastGlobalSequenceNumber";
        const long DefaultPosition = -1;

        readonly ViewDispatcherHelper<TViewInstance> _dispatcherHelper = new ViewDispatcherHelper<TViewInstance>();
        readonly MongoCollection<TViewInstance> _viewCollection;
        readonly MongoCollection<PositionDoc> _positionCollection;
        readonly ViewLocator _viewLocator = ViewLocator.GetLocatorFor<TViewInstance>();
        readonly Logger _logger = CirqusLoggerFactory.Current.GetCurrentClassLogger();
        readonly string _currentPositionDocId;

        long _cachedPosition;

        volatile bool _purging;

        public MongoDbViewManager(MongoDatabase database, string collectionName, string positionCollectionName = null)
        {
            positionCollectionName = positionCollectionName ?? collectionName + "Position";

            _viewCollection = database.GetCollection<TViewInstance>(collectionName);

            _logger.Info("Create index in '{0}': '{1}'", collectionName, CurrentPositionPropertyName);
            _viewCollection.CreateIndex(IndexKeys<TViewInstance>.Ascending(i => i.LastGlobalSequenceNumber), IndexOptions.SetName(CurrentPositionPropertyName));

            _positionCollection = database.GetCollection <PositionDoc>(positionCollectionName);
            _currentPositionDocId = string.Format("__{0}__position__", collectionName);
        }

        class PositionDoc
        {
            public string Id { get; set; }
            public long CurrentPosition { get; set; }
        }

        public MongoDbViewManager(string mongoDbConnectionString)
            : this(GetDatabaseFromConnectionString(mongoDbConnectionString))
        {
        }

        public MongoDbViewManager(string mongoDbConnectionString, string collectionName, string positionCollectionName = null)
            : this(GetDatabaseFromConnectionString(mongoDbConnectionString), collectionName, positionCollectionName)
        {
        }

        public MongoDbViewManager(MongoDatabase database)
            : this(database, typeof(TViewInstance).Name)
        {
        }

        public override TViewInstance Load(string viewId)
        {
            return _viewCollection.FindOneById(viewId);
        }

        public override void Delete(string viewId)
        {
            
        }

        public override string Id
        {
            get { return string.Format("{0}/{1}", typeof (TViewInstance).GetPrettyName(), _viewCollection); }
        }

        public override async Task<long> GetPosition(bool canGetFromCache = true)
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

        public override void Purge()
        {
            try
            {
                _purging = true;

                _logger.Info("Purging '{0}'", _viewCollection.Name);

                _viewCollection.RemoveAll();

                UpdatePersistentCache(DefaultPosition);

                Interlocked.Exchange(ref _cachedPosition, DefaultPosition);
            }
            finally
            {
                _purging = false;
            }
        }

        void UpdatePersistentCache(long newPosition)
        {
            _logger.Debug("Updating persistent position cache to {0}", newPosition);

            _positionCollection.Save(new PositionDoc
            {
                Id = _currentPositionDocId,
                CurrentPosition = newPosition
            });

            Interlocked.Exchange(ref _cachedPosition, newPosition);
        }

        public override void Dispatch(IViewContext viewContext, IEnumerable<DomainEvent> batch, IViewManagerProfiler viewManagerProfiler)
        {
            if (_purging) return;

            var cachedViewInstances = new Dictionary<string, TViewInstance>();

            var eventList = batch.ToList();

            if (!eventList.Any()) return;

            foreach (var e in eventList)
            {
                if (!ViewLocator.IsRelevant<TViewInstance>(e)) continue;

                var stopwatch = Stopwatch.StartNew();
                var viewIds = _viewLocator.GetAffectedViewIds(viewContext, e);

                foreach (var viewId in viewIds)
                {
                    var viewInstance = cachedViewInstances[viewId] = GetOrCreateViewInstance(viewId, cachedViewInstances);

                    _dispatcherHelper.DispatchToView(viewContext, e, viewInstance);
                }

                viewManagerProfiler.RegisterTimeSpent(this, e, stopwatch.Elapsed);
            }

            FlushCacheToDatabase(cachedViewInstances);

            RaiseUpdatedEventFor(cachedViewInstances.Values);

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
            var currentPositionDocument = _positionCollection
                .FindOneById(_currentPositionDocId);

            if (currentPositionDocument == null) return null;

            return currentPositionDocument.CurrentPosition;
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

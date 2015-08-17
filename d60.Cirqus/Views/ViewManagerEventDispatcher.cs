using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using d60.Cirqus.Aggregates;
using d60.Cirqus.Config;
using d60.Cirqus.Events;
using d60.Cirqus.Extensions;
using d60.Cirqus.Logging;
using d60.Cirqus.Serialization;
using d60.Cirqus.Views.ViewManagers;
using Timer = System.Timers.Timer;

namespace d60.Cirqus.Views
{
    /// <summary>
    /// Event dispatcher implementation that is capable of hosting any number of <see cref="IViewManager"/> implementations.
    /// A dedicated thread will dispatch events to the views as they happen, periodically checking in the background whether
    /// any of the views have got some catching up to do
    /// </summary>
    public class ViewManagerEventDispatcher : IDisposable, IAwaitableEventDispatcher
    {
        static Logger _logger;

        static ViewManagerEventDispatcher()
        {
            CirqusLoggerFactory.Changed += f => _logger = f.GetCurrentClassLogger();
        }

        readonly BackoffHelper _backoffHelper = new BackoffHelper(new[]
        {
            TimeSpan.FromMilliseconds(100),
            TimeSpan.FromMilliseconds(100),
            TimeSpan.FromMilliseconds(100),
            TimeSpan.FromMilliseconds(100),
            TimeSpan.FromMilliseconds(100),
            TimeSpan.FromMilliseconds(100),
            TimeSpan.FromMilliseconds(100),
            TimeSpan.FromMilliseconds(100),
            TimeSpan.FromMilliseconds(100),
            TimeSpan.FromMilliseconds(100),
            TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(5),
            TimeSpan.FromSeconds(5),
            TimeSpan.FromSeconds(5),
            TimeSpan.FromSeconds(5),
            TimeSpan.FromSeconds(5),
            TimeSpan.FromSeconds(5),
            TimeSpan.FromSeconds(30),
        });

        /// <summary>
        /// Use a concurrent queue to store views so that it's safe to traverse in the background even though new views may be added to it at runtime
        /// </summary>
        readonly ConcurrentDictionary<IViewManager, object> _viewManagers = new ConcurrentDictionary<IViewManager, object>();
        readonly ConcurrentQueue<PieceOfWork> _work = new ConcurrentQueue<PieceOfWork>();

        readonly IAggregateRootRepository _aggregateRootRepository;
        readonly IEventStore _eventStore;
        readonly IDomainEventSerializer _domainEventSerializer;
        readonly IDomainTypeNameMapper _domainTypeNameMapper;

        readonly Timer _automaticCatchUpTimer = new Timer();
        readonly Thread _worker;

        volatile bool _keepWorking = true;

        int _maxDomainEventsPerBatch = 100;

        long _sequenceNumberToCatchUpTo = -1;

        IViewManagerProfiler _viewManagerProfiler = new NullProfiler();

        /// <summary>
        /// Constructs the event dispatcher
        /// </summary>
        public ViewManagerEventDispatcher(IAggregateRootRepository aggregateRootRepository, IEventStore eventStore,
            IDomainEventSerializer domainEventSerializer, IDomainTypeNameMapper domainTypeNameMapper, params IViewManager[] viewManagers)
        {
            if (aggregateRootRepository == null) throw new ArgumentNullException("aggregateRootRepository");
            if (eventStore == null) throw new ArgumentNullException("eventStore");
            if (domainEventSerializer == null) throw new ArgumentNullException("domainEventSerializer");
            if (domainTypeNameMapper == null) throw new ArgumentNullException("domainTypeNameMapper");
            if (viewManagers == null) throw new ArgumentNullException("viewManagers");

            _aggregateRootRepository = aggregateRootRepository;
            _eventStore = eventStore;
            _domainEventSerializer = domainEventSerializer;
            _domainTypeNameMapper = domainTypeNameMapper;

            viewManagers.ToList().ForEach(AddViewManager);

            _worker = new Thread(DoWork) { IsBackground = true };

            _automaticCatchUpTimer.Elapsed += delegate
            {
                _work.Enqueue(PieceOfWork.FullCatchUp(false));
            };

            AutomaticCatchUpInterval = TimeSpan.FromSeconds(1);
        }

        /// <summary>
        /// Sets the profiler that the event dispatcher should use to aggregate timing information
        /// </summary>
        public void SetProfiler(IViewManagerProfiler viewManagerProfiler)
        {
            if (viewManagerProfiler == null) throw new ArgumentNullException("viewManagerProfiler");
            _logger.Info("Setting profiler: {0}", viewManagerProfiler);
            _viewManagerProfiler = viewManagerProfiler;
        }

        /// <summary>
        /// Adds the given view manager
        /// </summary>
        public void AddViewManager(IViewManager viewManager)
        {
            if (viewManager == null) throw new ArgumentNullException("viewManager");

            _viewManagers.AddOrUpdate(viewManager,
                v =>
                {
                    _logger.Debug("Added view manager: {0}", viewManager);
                    return new object();
                },
                (vm, existing) => existing);
        }

        /// <summary>
        /// Removed the given view manager
        /// </summary>
        public void RemoveViewManager(IViewManager viewManager)
        {
            if (viewManager == null) throw new ArgumentNullException("viewManager");

            object _;
            if (_viewManagers.TryRemove(viewManager, out _))
            {
                _logger.Debug("Removed view manager: {0}", viewManager);
            }
        }

        public void Initialize(IEventStore eventStore, bool purgeExistingViews = false)
        {
            if (eventStore == null) throw new ArgumentNullException("eventStore");
            _logger.Info("Initializing event dispatcher with view managers: {0}", string.Join(", ", _viewManagers));

            _logger.Debug("Initiating immediate full catchup");
            _work.Enqueue(PieceOfWork.FullCatchUp(purgeExistingViews: purgeExistingViews));

            _logger.Debug("Starting automatic catchup timer with {0} ms interval", _automaticCatchUpTimer.Interval);
            _automaticCatchUpTimer.Start();
            _worker.Start();
        }

        public void Dispatch(IEventStore eventStore, IEnumerable<DomainEvent> events)
        {
            if (eventStore == null) throw new ArgumentNullException("eventStore");
            if (events == null) throw new ArgumentNullException("events");
            var list = events.ToList();

            if (!list.Any()) return;

            var maxSequenceNumberInBatch = list.Max(e => e.GetGlobalSequenceNumber());

            Interlocked.Exchange(ref _sequenceNumberToCatchUpTo, maxSequenceNumberInBatch);

            _work.Enqueue(PieceOfWork.JustCatchUp(list));
        }

        /// <summary>
        /// Waits until the view(s) with the specified view instance type have successfully processed event at least up until those that were emitted
        /// as part of processing the command that yielded the given result
        /// </summary>
        public async Task WaitUntilProcessed<TViewInstance>(CommandProcessingResult result, TimeSpan timeout) where TViewInstance : IViewInstance
        {
            if (result == null) throw new ArgumentNullException("result");
            await Task.WhenAll(_viewManagers.Keys
                .OfType<IViewManager<TViewInstance>>()
                .Select(v => v.WaitUntilProcessed(result, timeout))
                .ToArray());
        }

        /// <summary>
        /// Waits until all view with the specified view instance type have successfully processed event at least up until those that were emitted
        /// as part of processing the command that yielded the given result
        /// </summary>
        public async Task WaitUntilProcessed(CommandProcessingResult result, TimeSpan timeout)
        {
            if (result == null) throw new ArgumentNullException("result");
            await Task.WhenAll(_viewManagers.Keys
                .Select(v => v.WaitUntilProcessed(result, timeout))
                .ToArray());
        }

        /// <summary>
        /// Gets/sets how many events to include at most in a batch between saving the state of view instances
        /// </summary>
        public int MaxDomainEventsPerBatch
        {
            get { return _maxDomainEventsPerBatch; }
            set
            {
                if (value < 1)
                {
                    throw new ArgumentException(string.Format("Attempted to set MAX items per batch to {0}! Please set it to at least 1...", value));
                }
                _maxDomainEventsPerBatch = value;
            }
        }

        /// <summary>
        /// Gets/sets the interval between automatically checking whether any views have got catching up to do
        /// </summary>
        public TimeSpan AutomaticCatchUpInterval
        {
            get { return TimeSpan.FromMilliseconds(_automaticCatchUpTimer.Interval); }
            set
            {
                if (value < TimeSpan.FromMilliseconds(1))
                {
                    throw new ArgumentException(string.Format("Attempted to set automatic catch-up interval to {0}! Please set it to at least 1 millisecond", value));
                }
                _automaticCatchUpTimer.Interval = value.TotalMilliseconds;

                _logger.Debug("Automatic catchup timer interval was set to {0} ms", _automaticCatchUpTimer.Interval);
            }
        }

        void DoWork()
        {
            _logger.Info("View manager background thread started");

            while (_keepWorking)
            {
                PieceOfWork pieceOfWork;
                if (!_work.TryDequeue(out pieceOfWork))
                {
                    Thread.Sleep(20);
                    continue;
                }

                var sequenceNumberToCatchUpTo = pieceOfWork.CatchUpAsFarAsPossible
                    ? long.MaxValue
                    : Interlocked.Read(ref _sequenceNumberToCatchUpTo);

                try
                {
                    CatchUpTo(sequenceNumberToCatchUpTo, _eventStore, pieceOfWork.CanUseCachedInformation, pieceOfWork.PurgeViewsFirst, _viewManagers.Keys.ToArray(), pieceOfWork.Events);

                    _backoffHelper.Reset();
                }
                catch (Exception exception)
                {
                    var timeToWait = _backoffHelper.GetTimeToWait();

                    if (sequenceNumberToCatchUpTo == long.MaxValue)
                    {
                        _logger.Warn(exception, "Could not catch up - waiting {0}", timeToWait);
                    }
                    else
                    {
                        _logger.Warn(exception, "Could not catch up to {0} - waiting {1}", sequenceNumberToCatchUpTo, timeToWait);
                    }

                    Thread.Sleep(timeToWait);
                }
            }

            _logger.Info("View manager background thread stopped!");
        }

        void CatchUpTo(long sequenceNumberToCatchUpTo, IEventStore eventStore, bool cachedInformationAllowed, bool purgeViewsFirst, IViewManager[] viewManagers, List<DomainEvent> events)
        {
            // bail out now if there isn't any actual work to do
            if (!viewManagers.Any()) return;

            if (purgeViewsFirst)
            {
                foreach (var viewManager in viewManagers)
                {
                    viewManager.Purge();
                }
            }

            // get the lowest position among all the view managers
            var positions = viewManagers
                .Select(viewManager => new Pos(viewManager, viewManager.GetPosition(canGetFromCache: cachedInformationAllowed).Result))
                .ToDictionary(a => a.ViewManager);

            var lowestSequenceNumberSuccessfullyProcessed = positions.Min(a => a.Value.Position);

            // if we've already been there, don't do anything
            if (lowestSequenceNumberSuccessfullyProcessed >= sequenceNumberToCatchUpTo) return;

            // if we can dispatch events directly, we do it now
            if (events.Any() && lowestSequenceNumberSuccessfullyProcessed >= events.First().GetGlobalSequenceNumber() - 1)
            {
                var serializedEvents = events.Select(e => _domainEventSerializer.Serialize(e));

                DispatchBatchToViewManagers(viewManagers, serializedEvents, positions);

                lowestSequenceNumberSuccessfullyProcessed = events.Last().GetGlobalSequenceNumber();

                // if we've done enough, quit now
                if (lowestSequenceNumberSuccessfullyProcessed >= sequenceNumberToCatchUpTo) return;
            }

            // ok, we must replay - start from here:
            var sequenceNumberToReplayFrom = lowestSequenceNumberSuccessfullyProcessed + 1;

            foreach (var batch in eventStore.Stream(sequenceNumberToReplayFrom).Batch(MaxDomainEventsPerBatch))
            {
                DispatchBatchToViewManagers(viewManagers, batch, positions);
            }
        }

        class Pos
        {
            public Pos(IViewManager viewManager, long position)
            {
                ViewManager = viewManager;
                Position = position;
            }

            public IViewManager ViewManager { get; private set; }
            public long Position { get; private set; }
        }

        void DispatchBatchToViewManagers(IEnumerable<IViewManager> viewManagers, IEnumerable<EventData> batch, Dictionary<IViewManager, Pos> positions)
        {
            var context = new DefaultViewContext(_aggregateRootRepository, _domainTypeNameMapper);
            var eventList = batch
                .Select(e => _domainEventSerializer.Deserialize(e))
                .ToList();

            foreach (var viewManager in viewManagers)
            {
                var thisParticularPosition = positions[viewManager].Position;
                if (thisParticularPosition >= eventList.Max(e => e.GetGlobalSequenceNumber())) continue;

                _logger.Debug("Dispatching batch of {0} events to {1}", eventList.Count, viewManager);

                viewManager.Dispatch(context, eventList, _viewManagerProfiler);
            }
        }

        class PieceOfWork
        {
            PieceOfWork()
            {
                Events = new List<DomainEvent>();
            }

            public static PieceOfWork FullCatchUp(bool purgeExistingViews)
            {
                return new PieceOfWork
                {
                    CatchUpAsFarAsPossible = true,
                    CanUseCachedInformation = false,
                    PurgeViewsFirst = purgeExistingViews
                };
            }

            public static PieceOfWork JustCatchUp(List<DomainEvent> recentlyEmittedEvents)
            {
                return new PieceOfWork
                {
                    CatchUpAsFarAsPossible = false,
                    CanUseCachedInformation = true,
                    PurgeViewsFirst = false,
                    Events = recentlyEmittedEvents
                };
            }

            public List<DomainEvent> Events { get; private set; }

            public bool CatchUpAsFarAsPossible { get; private set; }

            public bool CanUseCachedInformation { get; private set; }

            public bool PurgeViewsFirst { get; private set; }

            public override string ToString()
            {
                return string.Format("Catch up {0} (allow cache: {1}, purge: {2})",
                    CatchUpAsFarAsPossible
                        ? "to MAX"
                        : "to latest",
                    CanUseCachedInformation, PurgeViewsFirst);
            }
        }

        bool _disposed;

        ~ViewManagerEventDispatcher()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Stops the background timer and shuts down the worker thread
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                _keepWorking = false;

                try
                {
                    _automaticCatchUpTimer.Stop();
                    _automaticCatchUpTimer.Dispose();
                }
                catch
                {
                }

                try
                {
                    if (!_worker.Join(TimeSpan.FromSeconds(4)))
                    {
                        _logger.Warn("Worker thread did not stop within 4 second timeout!");
                    }
                }
                catch
                {
                }
            }

            _disposed = true;
        }

        public IEnumerable<IViewManager> GetViewManagers()
        {
            return _viewManagers.Keys;
        }
    }
}
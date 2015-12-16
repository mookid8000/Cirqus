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
    /// Special view manager event dispatcher that can have other views as a dependency, causing it to catch up to those views instead of catching up to the event store
    /// </summary>
    public class DependentViewManagerEventDispatcher : IEventDispatcher, IDisposable, IAwaitableEventDispatcher
    {
        static Logger _logger;

        static DependentViewManagerEventDispatcher()
        {
            CirqusLoggerFactory.Changed += f => _logger = f.GetCurrentClassLogger();
        }

        readonly BackoffHelper _backoffHelper = new BackoffHelper(new[]
        {
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

        readonly ConcurrentQueue<Work> _work = new ConcurrentQueue<Work>();
        readonly Timer _automaticCatchUpTimer = new Timer();
        readonly List<IViewManager> _dependencies;
        readonly List<IViewManager> _viewManagers;
        readonly IEventStore _eventStore;
        readonly IDomainEventSerializer _domainEventSerializer;
        readonly IAggregateRootRepository _aggregateRootRepository;
        readonly IDomainTypeNameMapper _domainTypeNameMapper;
        readonly Dictionary<string, object> _viewContextItems;
        readonly Thread _workerThread;

        volatile bool _keepWorking = true;

        IViewManagerProfiler _viewManagerProfiler = new NullProfiler();
        int _maxDomainEventsPerBatch;

        /// <summary>
        /// Constructs the event dispatcher
        /// </summary>
        public DependentViewManagerEventDispatcher(IEnumerable<IViewManager> dependencies, IEnumerable<IViewManager> viewManagers, IEventStore eventStore, IDomainEventSerializer domainEventSerializer, IAggregateRootRepository aggregateRootRepository, IDomainTypeNameMapper domainTypeNameMapper, Dictionary<string, object> viewContextItems)
        {
            _dependencies = dependencies.ToList();
            _viewManagers = viewManagers.ToList();
            _eventStore = eventStore;
            _domainEventSerializer = domainEventSerializer;
            _aggregateRootRepository = aggregateRootRepository;
            _domainTypeNameMapper = domainTypeNameMapper;
            _viewContextItems = viewContextItems;
            _workerThread = new Thread(DoWork) {IsBackground = true};

            _automaticCatchUpTimer.Elapsed += (o, ea) => _work.Enqueue(new Work());
            _automaticCatchUpTimer.Interval = 1000;
            _maxDomainEventsPerBatch = 100;
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
        /// Sets the profiler that the event dispatcher should use to aggregate timing information
        /// </summary>
        public void SetProfiler(IViewManagerProfiler viewManagerProfiler)
        {
            if (viewManagerProfiler == null) throw new ArgumentNullException("viewManagerProfiler");
            _logger.Info("Setting profiler: {0}", viewManagerProfiler);
            _viewManagerProfiler = viewManagerProfiler;
        }

        void DoWork()
        {
            _logger.Info("Starting dependent view manager event dispatcher (hosting {0}, dependent on {1})",
                string.Join(", ", _viewManagers.Select(v => v.GetType().GetPrettyName())),
                string.Join(", ", _dependencies.Select(v => v.GetType().GetPrettyName())));

            while (_keepWorking)
            {
                Thread.Sleep(100);

                Work work;

                if (!_work.TryDequeue(out work))
                {
                    Thread.Sleep(20);
                    continue;
                }

                try
                {
                    CatchUp();

                    _backoffHelper.Reset();
                }
                catch (Exception exception)
                {
                    var timeToWait = _backoffHelper.GetTimeToWait();

                    _logger.Warn(exception, "Could not catch up - waiting {0}", timeToWait);
                    
                    Thread.Sleep(timeToWait);
                }
            }
        }

        void CatchUp()
        {
            if (!_viewManagers.Any())
            {
                Thread.Sleep(1000);
                return;
            }

            var sequenceNumberToCatchUpTo = GetPosition(_dependencies, _eventStore.GetNextGlobalSequenceNumber() - 1);

            var positions = _viewManagers
                .Select(viewManager => new Pos(viewManager, viewManager.GetPosition().Result))
                .ToDictionary(a => a.ViewManager);

            var currentPosition = positions.Min(a => a.Value.Position);

            if (currentPosition >= sequenceNumberToCatchUpTo)
            {
                return;
            }

            var relevantEventBatches = _eventStore
                .Stream(currentPosition)
                .TakeWhile(e => e.GetGlobalSequenceNumber() <= sequenceNumberToCatchUpTo)
                .Batch(MaxDomainEventsPerBatch)
                .Select(b => b.ToList());

            foreach (var e in relevantEventBatches)
            {
                DispatchBatchToViewManagers(_viewManagers, e, positions);
            }
        }

        void DispatchBatchToViewManagers(IEnumerable<IViewManager> viewManagers, IEnumerable<EventData> batch, Dictionary<IViewManager, Pos> positions)
        {
            var eventList = batch
                .Select(e => _domainEventSerializer.Deserialize(e))
                .ToList();

            var context = new DefaultViewContext(_aggregateRootRepository, _domainTypeNameMapper, eventList);

            foreach (var kvp in _viewContextItems)
            {
                context.Items[kvp.Key] = kvp.Value;
            }

            foreach (var viewManager in viewManagers)
            {
                var thisParticularPosition = positions[viewManager].Position;
                if (thisParticularPosition >= eventList.Max(e => e.GetGlobalSequenceNumber())) continue;

                _logger.Debug("Dispatching batch of {0} events to {1}", eventList.Count, viewManager);

                viewManager.Dispatch(context, eventList, _viewManagerProfiler);
            }
        }

        static long GetPosition(List<IViewManager> viewManagers, long defaultValue)
        {
            if (!viewManagers.Any()) return defaultValue;

            var positionTasks = viewManagers.Select(d => d.GetPosition()).ToArray();
            
            Task.WaitAll(positionTasks);
            
            var sequenceNumberToCatchUpTo = positionTasks.Min(t => t.Result);

            return sequenceNumberToCatchUpTo;
        }

        public void Initialize(IEventStore eventStore, bool purgeExistingViews = false)
        {
            _work.Enqueue(new Work());
            _workerThread.Start();
            _automaticCatchUpTimer.Start();
        }

        class Work { }

        public void Dispatch(IEventStore eventStore, IEnumerable<DomainEvent> events)
        {
            _work.Enqueue(new Work());
        }

        public void Dispose()
        {
            _logger.Info("Stopping dependent view manager event dispatcher");

            _automaticCatchUpTimer.Dispose();

            _keepWorking = false;
            if (!_workerThread.Join(TimeSpan.FromSeconds(5)))
            {
                _logger.Warn("Did not stop within 5 second timeout!");
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

        public async Task WaitUntilProcessed<TViewInstance>(CommandProcessingResult result, TimeSpan timeout) where TViewInstance : IViewInstance
        {
            if (result == null) throw new ArgumentNullException("result");
            await Task.WhenAll(_viewManagers
                .OfType<IViewManager<TViewInstance>>()
                .Select(v => v.WaitUntilProcessed(result, timeout))
                .ToArray());
        }

        public async Task WaitUntilProcessed(CommandProcessingResult result, TimeSpan timeout)
        {
            if (result == null) throw new ArgumentNullException("result");
            await Task.WhenAll(_viewManagers
                .Select(v => v.WaitUntilProcessed(result, timeout))
                .ToArray());
        }
    }
}
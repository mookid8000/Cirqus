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
    public class ViewManagerEventDispatcher : IEventDispatcher, IDisposable
    {
        static Logger _logger;

        static ViewManagerEventDispatcher()
        {
            CirqusLoggerFactory.Changed += f => _logger = f.GetCurrentClassLogger();
        }

        /// <summary>
        /// Use a concurrent queue to store views so that it's safe to traverse in the background even though new views may be added to it at runtime
        /// </summary>
        readonly ConcurrentQueue<IViewManager> _viewManagers = new ConcurrentQueue<IViewManager>();

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

        public ViewManagerEventDispatcher(IAggregateRootRepository aggregateRootRepository, IEventStore eventStore, IDomainEventSerializer domainEventSerializer, IDomainTypeNameMapper domainTypeNameMapper, params IViewManager[] viewManagers)
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

            viewManagers.ToList().ForEach(view => _viewManagers.Enqueue(view));

            _worker = new Thread(DoWork) { IsBackground = true };

            _automaticCatchUpTimer.Elapsed += delegate
            {
                _work.Enqueue(PieceOfWork.FullCatchUp(false));
            };

            AutomaticCatchUpInterval = TimeSpan.FromSeconds(1);
        }

        public void AddViewManager(IViewManager viewManager)
        {
            _logger.Info("Adding view manager: {0}", viewManager);

            _viewManagers.Enqueue(viewManager);
        }

        public void Initialize(IEventStore eventStore, bool purgeExistingViews = false)
        {
            _logger.Info("Initializing event dispatcher with view managers: {0}", string.Join(", ", _viewManagers));

            _logger.Debug("Initiating immediate full catchup");
            _work.Enqueue(PieceOfWork.FullCatchUp(purgeExistingViews: purgeExistingViews));

            _logger.Debug("Starting automatic catchup timer with {0} ms interval", _automaticCatchUpTimer.Interval);
            _automaticCatchUpTimer.Start();
            _worker.Start();
        }

        public void Dispatch(IEventStore eventStore, IEnumerable<DomainEvent> events)
        {
            var list = events.ToList();

            if (!list.Any()) return;

            var maxSequenceNumberInBatch = list.Max(e => e.GetGlobalSequenceNumber());

            Interlocked.Exchange(ref _sequenceNumberToCatchUpTo, maxSequenceNumberInBatch);

            _work.Enqueue(PieceOfWork.JustCatchUp(list));
        }

        public async Task WaitUntilProcessed<TViewInstance>(CommandProcessingResult result, TimeSpan timeout) where TViewInstance : IViewInstance
        {
            await Task.WhenAll(_viewManagers
                .OfType<IViewManager<TViewInstance>>()
                .Select(v => v.WaitUntilProcessed(result, timeout))
                .ToArray());
        }

        public async Task WaitUntilProcessed(CommandProcessingResult result, TimeSpan timeout)
        {
            await Task.WhenAll(_viewManagers
                .Select(v => v.WaitUntilProcessed(result, timeout))
                .ToArray());
        }

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
                    Thread.Sleep(100);
                    continue;
                }

                var sequenceNumberToCatchUpTo = pieceOfWork.CatchUpAsFarAsPossible
                    ? long.MaxValue
                    : Interlocked.Read(ref _sequenceNumberToCatchUpTo);

                try
                {
                    CatchUpTo(sequenceNumberToCatchUpTo, _eventStore, pieceOfWork.CanUseCachedInformation, pieceOfWork.PurgeViewsFirst, _viewManagers.ToArray(), pieceOfWork.Events);
                }
                catch (Exception exception)
                {
                    _logger.Warn(exception, "Could not catch up to {0}", sequenceNumberToCatchUpTo);
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
            var lowestSequenceNumberSuccessfullyProcessed = viewManagers
                .Min(v => v.GetPosition(canGetFromCache: cachedInformationAllowed));

            // if we've already been there, don't do anything
            if (lowestSequenceNumberSuccessfullyProcessed >= sequenceNumberToCatchUpTo) return;

            // if we can dispatch events directly, we do it now
            if (events.Any() && lowestSequenceNumberSuccessfullyProcessed >= events.First().GetGlobalSequenceNumber() - 1)
            {
                var serializedEvents = events.Select(e => _domainEventSerializer.Serialize(e));

                Console.WriteLine("DISPATCHING DIRECTLY: {0}", string.Join(",", events.Select(e => e.GetGlobalSequenceNumber())));
                DispatchBatchToViewManagers(viewManagers, serializedEvents);

                lowestSequenceNumberSuccessfullyProcessed = events.Last().GetGlobalSequenceNumber();

                // if we've done enough, quit now
                if (lowestSequenceNumberSuccessfullyProcessed >= sequenceNumberToCatchUpTo) return;
            }

            // ok, we must replay - start from here:
            var sequenceNumberToReplayFrom = lowestSequenceNumberSuccessfullyProcessed + 1;

            foreach (var batch in eventStore.Stream(sequenceNumberToReplayFrom).Batch(MaxDomainEventsPerBatch))
            {
                DispatchBatchToViewManagers(viewManagers, batch);
            }
        }

        void DispatchBatchToViewManagers(IEnumerable<IViewManager> viewManagers, IEnumerable<EventData> batch)
        {
            var context = new DefaultViewContext(_aggregateRootRepository, _domainTypeNameMapper);
            var list = batch.ToList();

            foreach (var viewManager in viewManagers)
            {
                _logger.Debug("Dispatching batch of {0} events to {1}", list.Count, viewManager);

                viewManager.Dispatch(context, list.Select(e => _domainEventSerializer.Deserialize(e)));
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

            public List<DomainEvent> Events { get; set; }

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
                    _worker.Join(TimeSpan.FromSeconds(1));
                }
                catch
                {
                }
            }

            _disposed = true;
        }
    }
}
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using d60.Cirqus.Aggregates;
using d60.Cirqus.Events;
using d60.Cirqus.Extensions;
using d60.Cirqus.Logging;
using Timer = System.Timers.Timer;

namespace d60.Cirqus.Views.ViewManagers
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
        readonly ConcurrentQueue<IManagedView> _managedViews = new ConcurrentQueue<IManagedView>();

        readonly ConcurrentQueue<PieceOfWork> _work = new ConcurrentQueue<PieceOfWork>();

        readonly IAggregateRootRepository _aggregateRootRepository;
        readonly IEventStore _eventStore;

        readonly Timer _automaticCatchUpTimer = new Timer();
        readonly Thread _worker;

        volatile bool _keepWorking = true;
        int _maxItemsPerBatch = 100;
        TimeSpan _automaticCatchUpInterval = TimeSpan.FromSeconds(1);
        long _sequenceNumberToCatchUpTo = -1;

        public ViewManagerEventDispatcher(IAggregateRootRepository aggregateRootRepository, IEventStore eventStore, params IManagedView[] managedViews)
        {
            _aggregateRootRepository = aggregateRootRepository;
            _eventStore = eventStore;

            managedViews.ToList().ForEach(view => _managedViews.Enqueue(view));

            _worker = new Thread(DoWork) { IsBackground = true };

            _automaticCatchUpTimer.Interval = _automaticCatchUpInterval.TotalMilliseconds;
            _automaticCatchUpTimer.Elapsed += delegate
            {
                _work.Enqueue(PieceOfWork.FullCatchUp(false));
            };
        }

        public void AddViewManager(IManagedView managedView)
        {
            _logger.Info("Adding managed view: {0}", managedView);

            _managedViews.Enqueue(managedView);
        }

        public void Initialize(IEventStore eventStore, bool purgeExistingViews = false)
        {
            _logger.Info("Initializing view manager with managed views: {0}", string.Join(", ", _managedViews));

            _work.Enqueue(PieceOfWork.FullCatchUp(purgeExistingViews: purgeExistingViews));

            _automaticCatchUpTimer.Start();
            _worker.Start();
        }

        public void Dispatch(IEventStore eventStore, IEnumerable<DomainEvent> events)
        {
            var list = events.ToList();

            if (!list.Any()) return;

            var maxSequenceNumberInBatch = list.Max(e => e.GetGlobalSequenceNumber());

            Interlocked.Exchange(ref _sequenceNumberToCatchUpTo, maxSequenceNumberInBatch);

            _work.Enqueue(PieceOfWork.JustCatchUp());
        }

        public async Task WaitUntilProcessed<TViewInstance>(CommandProcessingResult result, TimeSpan timeout) where TViewInstance : IViewInstance
        {
            await Task.WhenAll(_managedViews
                .OfType<IManagedView<TViewInstance>>()
                .Select(v => v.WaitUntilProcessed(result, timeout))
                .ToArray());
        }

        public async Task WaitUntilProcessed(CommandProcessingResult result, TimeSpan timeout)
        {
            await Task.WhenAll(_managedViews
                .Select(v => v.WaitUntilProcessed(result, timeout))
                .ToArray());
        }

        public int MaxItemsPerBatch
        {
            get { return _maxItemsPerBatch; }
            set
            {
                if (value < 1)
                {
                    throw new ArgumentException(string.Format("Attempted to set MAX items per batch to {0}! Please set it to at least 1...", value));
                }
                _maxItemsPerBatch = value;
            }
        }

        public TimeSpan AutomaticCatchUpInterval
        {
            get { return _automaticCatchUpInterval; }
            set
            {
                if (value < TimeSpan.FromMilliseconds(1))
                {
                    throw new ArgumentException(string.Format("Attempted to set automatic catch-up interval to {0}! Please set it to at least 1 millisecond", value));
                }
                _automaticCatchUpInterval = value;
                _automaticCatchUpTimer.Interval = value.TotalMilliseconds;
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
                    CatchUpTo(sequenceNumberToCatchUpTo, _eventStore, pieceOfWork.CanUseCachedInformation, pieceOfWork.PurgeViewsFirst, _managedViews.ToArray());
                }
                catch (Exception exception)
                {
                    _logger.Warn(exception, "Could not catch up to {0}", sequenceNumberToCatchUpTo);
                }
            }

            _logger.Info("View manager background thread stopped!");
        }

        void CatchUpTo(long sequenceNumberToCatchUpTo, IEventStore eventStore, bool cachedInformationAllowed, bool purgeViewsFirst, IManagedView[] managedViews)
        {
            // bail out now if there isn't any actual work to do
            if (!managedViews.Any()) return;

            if (purgeViewsFirst)
            {
                foreach (var managedView in managedViews)
                {
                    managedView.Purge();
                }
            }

            // get the lowest position among all the managed views
            var lowestSequenceNumberSuccessfullyProcessed = managedViews
                .Min(v => v.GetPosition(canGetFromCache: cachedInformationAllowed));

            // if we've already been there, don't do anything
            if (lowestSequenceNumberSuccessfullyProcessed >= sequenceNumberToCatchUpTo) return;

            // ok, we must replay - start from here:
            var sequenceNumberToReplayFrom = lowestSequenceNumberSuccessfullyProcessed + 1;

            _logger.Debug("Getting events from global sequence number {0} and on...", sequenceNumberToReplayFrom);

            foreach (var batch in eventStore.Stream(sequenceNumberToReplayFrom).Batch(MaxItemsPerBatch))
            {
                var context = new DefaultViewContext(_aggregateRootRepository);
                var list = batch.ToList();

                foreach (var managedView in managedViews)
                {
                    _logger.Debug("Dispatching batch of {0} events to {1}", list.Count, managedView);

                    managedView.Dispatch(context, list);
                }
            }
        }

        class PieceOfWork
        {
            PieceOfWork()
            {                
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

            public static PieceOfWork JustCatchUp()
            {
                return new PieceOfWork
                {
                    CatchUpAsFarAsPossible = false,
                    CanUseCachedInformation = true,
                    PurgeViewsFirst = false
                };
            }

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
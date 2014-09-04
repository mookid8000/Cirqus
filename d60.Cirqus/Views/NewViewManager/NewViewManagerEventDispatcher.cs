using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using d60.Cirqus.Aggregates;
using d60.Cirqus.Events;
using d60.Cirqus.Extensions;
using d60.Cirqus.Logging;
using Timer = System.Timers.Timer;

namespace d60.Cirqus.Views.NewViewManager
{
    public class NewViewManagerEventDispatcher : IEventDispatcher
    {
        static Logger _logger;

        static NewViewManagerEventDispatcher()
        {
            CirqusLoggerFactory.Changed += f => _logger = f.GetCurrentClassLogger();
        }

        readonly ConcurrentQueue<PieceOfWork> _sequenceNumbersToCatchUpTo = new ConcurrentQueue<PieceOfWork>();
        readonly IAggregateRootRepository _aggregateRootRepository;
        readonly Timer _automaticCatchUpTimer = new Timer();
        readonly IEventStore _eventStore;
        readonly List<IManagedView> _managedViews;
        readonly Thread _worker;

        volatile bool _keepWorking = true;

        public NewViewManagerEventDispatcher(IAggregateRootRepository aggregateRootRepository, IEventStore eventStore, params IManagedView[] managedViews)
        {
            _aggregateRootRepository = aggregateRootRepository;
            _eventStore = eventStore;
            _managedViews = managedViews.ToList();

            _worker = new Thread(DoWork) { IsBackground = true };

            _automaticCatchUpTimer.Interval = 5000;
            _automaticCatchUpTimer.Elapsed += delegate
            {
                _sequenceNumbersToCatchUpTo.Enqueue(new PieceOfWork(long.MaxValue, _eventStore));
            };
        }

        ~NewViewManagerEventDispatcher()
        {
            _keepWorking = false;

            try
            {
                _automaticCatchUpTimer.Stop();
                _automaticCatchUpTimer.Dispose();
            }
            catch { }

            try
            {
                _worker.Join(TimeSpan.FromSeconds(1));
            }
            catch { }
        }

        public void Initialize(IEventStore eventStore, bool purgeExistingViews = false)
        {
            _sequenceNumbersToCatchUpTo.Enqueue(new PieceOfWork(long.MaxValue, _eventStore));

            _automaticCatchUpTimer.Start();
            _worker.Start();
        }

        public void Dispatch(IEventStore eventStore, IEnumerable<DomainEvent> events)
        {
            var list = events.ToList();

            if (!list.Any()) return;

            var maxSequenceNumberInBatch = list.Max(e => e.GetGlobalSequenceNumber());

            _sequenceNumbersToCatchUpTo.Enqueue(new PieceOfWork(maxSequenceNumberInBatch, eventStore));
        }

        class PieceOfWork
        {
            public PieceOfWork(long sequenceNumberToCatchUpTo, IEventStore eventStore)
            {
                SequenceNumberToCatchUpTo = sequenceNumberToCatchUpTo;
                EventStore = eventStore;
            }

            public long SequenceNumberToCatchUpTo { get; private set; }

            public IEventStore EventStore { get; private set; }
        }

        void DoWork()
        {
            while (_keepWorking)
            {
                PieceOfWork pieceOfWork;
                if (!_sequenceNumbersToCatchUpTo.TryDequeue(out pieceOfWork))
                {
                    Thread.Sleep(100);
                    continue;
                }

                try
                {
                    CatchUpTo(pieceOfWork.SequenceNumberToCatchUpTo, pieceOfWork.EventStore);
                }
                catch (Exception exception)
                {
                    _logger.Warn(exception, "Could not catch up to {0}", pieceOfWork.SequenceNumberToCatchUpTo);
                }
            }
        }

        void CatchUpTo(long sequenceNumberToCatchUpTo, IEventStore eventStore)
        {
            if (!_managedViews.Any()) return;

            var lowestSequenceNumnerSuccessfullyProcessed = _managedViews.Min(v => v.GetLowWatermark());

            if (lowestSequenceNumnerSuccessfullyProcessed >= sequenceNumberToCatchUpTo) return;

            var sequenceNumberToReplayFrom = lowestSequenceNumnerSuccessfullyProcessed + 1;

            _logger.Debug("Automatic replay from {0} and on...", sequenceNumberToReplayFrom);

            foreach (var batch in eventStore.Stream(sequenceNumberToReplayFrom).Batch(100))
            {
                var context = new DefaultViewContext(_aggregateRootRepository);
                var list = batch.ToList();

                foreach (var managedView in _managedViews)
                {
                    _logger.Debug("Dispatching batch of {0} events to {1}", list.Count, managedView);
                    
                    managedView.Dispatch(context, list);
                }
            }
        }
    }
}
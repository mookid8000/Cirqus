using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using d60.Cirqus.Extensions;
using d60.Cirqus.Logging;

namespace d60.Cirqus.Events
{
    /// <summary>
    /// Replicator that can sync events from one event store to another. PLEASE NOTE that it is assumed that the destination
    /// event store does not receive events from anywhere else, since the destinations event store's <see cref="IEventStore.GetNextGlobalSequenceNumber"/>
    /// is used to figure out which sequence number to resume from in the source event store.
    /// </summary>
    public class EventReplicator : IDisposable
    {
        public const string SourceEventBatchId = "src_batch_id";

        static Logger _logger;

        static EventReplicator()
        {
            CirqusLoggerFactory.Changed += f => _logger = f.GetCurrentClassLogger();
        }

        readonly IEventStore _sourceEventStore;
        readonly IEventStore _destinationEventStore;
        readonly Thread _workerThread;

        volatile bool _stop;
        volatile bool _running;

        public EventReplicator(IEventStore sourceEventStore, IEventStore destinationEventStore)
        {
            _sourceEventStore = sourceEventStore;
            _destinationEventStore = destinationEventStore;

            _workerThread = new Thread(Replicate)
            {
                IsBackground = true,
                Name = "EventReplicator worker"
            };

            TimeToPauseOnError = TimeSpan.FromSeconds(10);
            MaxEventsPerBatch = 100;
        }

        public void Start()
        {
            _logger.Info("Starting replicator worker thread...");
            _workerThread.Start();
            _logger.Info("Started!");
        }

        void Replicate()
        {
            _running = true;

            while (!_stop)
            {
                try
                {
                    PumpEvents();
                }
                catch (Exception exception)
                {
                    _logger.Error(exception, "An error occurred while attempting to load events from {0} into {1} - waiting {2}",
                        _sourceEventStore, _destinationEventStore, TimeToPauseOnError);

                    // avoid thrashing
                    Thread.Sleep(TimeToPauseOnError);
                }
            }
        }

        public TimeSpan TimeToPauseOnError { get; set; }

        public int MaxEventsPerBatch { get; set; }

        void PumpEvents()
        {
            var didGetEvents = false;
            var nextEventBatch = new List<EventData>();

            foreach (var newEvent in _sourceEventStore.Stream(_destinationEventStore.GetNextGlobalSequenceNumber()))
            {
                newEvent.Meta[SourceEventBatchId] = newEvent.GetBatchId().ToString();

                _logger.Debug("Replicating event {0}", newEvent.GetGlobalSequenceNumber());

                nextEventBatch.Add(newEvent);

                if (nextEventBatch.Count >= MaxEventsPerBatch)
                {
                    SaveBatch(nextEventBatch);
                    nextEventBatch = new List<EventData>();
                }

                didGetEvents = true;
            }

            if (nextEventBatch.Any())
            {
                SaveBatch(nextEventBatch);
            }

            if (!didGetEvents)
            {
                Thread.Sleep(200);
            }
        }

        void SaveBatch(List<EventData> nextEventBatch)
        {
            var newEventBatchId = Guid.NewGuid();
            _destinationEventStore.Save(newEventBatchId, nextEventBatch);
        }

        bool _disposed;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;

            try
            {
                if (disposing)
                {
                    if (!_running) return;

                    _stop = true;

                    _logger.Info("Stopping replicator worker thread...");

                    var timeout = TimeSpan.FromSeconds(5);
                    if (!_workerThread.Join(timeout))
                    {
                        _logger.Warn("Worker thread did not stop within {0} timeout!", timeout);
                    }
                    else
                    {
                        _logger.Info("Stopped!");
                    }
                }
            }
            finally
            {
                _disposed = true;
            }
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using d60.Cirqus.Extensions;
using d60.Cirqus.Logging;
using Timer = System.Timers.Timer;

namespace d60.Cirqus.Events
{
    /// <summary>
    /// Replicator that can sync events from one event store to another. PLEASE NOTE that it is assumed that the destination
    /// event store does not receive events from anywhere else, since the destinations event store's <see cref="IEventStore.GetNextGlobalSequenceNumber"/>
    /// is used to figure out which sequence number to resume from in the source event store.
    /// </summary>
    public class EventReplicator : IDisposable
    {
        const int StatsIntervalSeconds = 60;

        /// <summary>
        /// Metadata key of the source event batch ID
        /// </summary>
        public const string SourceEventBatchId = "src_batch_id";

        /// <summary>
        /// The default number of events to include in each event batch in the destnation event store.
        /// </summary>
        public const int DefaultMaxEventsPerBatch = 1;

        static Logger _logger;

        static EventReplicator()
        {
            CirqusLoggerFactory.Changed += f => _logger = f.GetCurrentClassLogger();
        }

        readonly Timer _statsTimer = new Timer(StatsIntervalSeconds * 1000);
        readonly IEventStore _sourceEventStore;
        readonly IEventStore _destinationEventStore;
        readonly Thread _workerThread;

        volatile bool _stop;
        volatile bool _running;

        long _replicatedEvents;
        bool _disposed;

        /// <summary>
        /// Constructs the replicator with the given source and destination event stored. Replication is not started until <see cref="Start"/> is called though.
        /// </summary>
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
            MaxEventsPerBatch = DefaultMaxEventsPerBatch;

            _statsTimer.Elapsed += (o, ea) => DumpStats();
        }

        /// <summary>
        /// Starts the replicator
        /// </summary>
        public void Start()
        {
            _logger.Info("Starting replicator worker thread...");
            _workerThread.Start();
            _statsTimer.Start();
            _logger.Info("Started!");
        }

        void DumpStats()
        {
            var replicatedEventsSinceLastDump = Interlocked.Exchange(ref _replicatedEvents, 0);

            if (replicatedEventsSinceLastDump == 0)
            {
                _logger.Info("No events were replicated the last {0} seconds", StatsIntervalSeconds);
            }
            else
            {
                _logger.Info("{0} events were replicated the last {1} seconds", replicatedEventsSinceLastDump, StatsIntervalSeconds);
            }
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

        /// <summary>
        /// Gets/sets how long to take a break if there is an error
        /// </summary>
        public TimeSpan TimeToPauseOnError { get; set; }

        /// <summary>
        /// Gets/sets how many events to put in a batch in the destination event store. Defaults to <see cref="DefaultMaxEventsPerBatch"/>.
        /// WARNING: This might/might not have unintended consequences, so please think it through before you increase this value.
        /// For example, with the MongoDB event store, large event batches will result in unnecessarily high memory usage when loading events for aggregate
        /// roots, but it can increase throughput when streaming events when replaying projections.
        /// </summary>
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

        void SaveBatch(List<EventData> eventBatch)
        {
            var newEventBatchId = Guid.NewGuid();

            _destinationEventStore.Save(newEventBatchId, eventBatch);

            Interlocked.Add(ref _replicatedEvents, eventBatch.Count);
        }

        /// <summary>
        /// Stops the worker thread and the stats timer
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;

            try
            {
                if (!_running) return;

                _statsTimer.Dispose();
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

                DumpStats();
            }
            finally
            {
                _disposed = true;
            }
        }
    }
}
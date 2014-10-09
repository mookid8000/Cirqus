using System;
using System.Threading;
using d60.Cirqus.Extensions;
using d60.Cirqus.Logging;

namespace d60.Cirqus.Events
{
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
                    _logger.Error(exception, "An error occurred while attempting to load events from {0} into {1}", _sourceEventStore, _destinationEventStore);
                }
            }
        }

        void PumpEvents()
        {
            var didGetEvents = false;

            foreach (var newEvent in _sourceEventStore.Stream(_destinationEventStore.GetNextGlobalSequenceNumber()))
            {
                var newEventBatchId = Guid.NewGuid();

                newEvent.Meta[SourceEventBatchId] = newEvent.GetBatchId();

                _logger.Debug("Replicating event {0}", newEvent.GetGlobalSequenceNumber());

                _destinationEventStore.Save(newEventBatchId, new[] { newEvent });
                didGetEvents = true;
            }

            if (!didGetEvents)
            {
                Thread.Sleep(200);
            }
        }

        public void Dispose()
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
}
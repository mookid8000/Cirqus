using System;
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
using d60.Cirqus.Views;
using d60.Cirqus.Views.ViewManagers;

namespace d60.Cirqus.Tests.Views
{
    public class DependentViewManagerEventDispatcher : IEventDispatcher, IDisposable, IAwaitableEventDispatcher
    {
        static Logger _logger;

        static DependentViewManagerEventDispatcher()
        {
            CirqusLoggerFactory.Changed += f => _logger = f.GetCurrentClassLogger();
        }

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

        public DependentViewManagerEventDispatcher(IEnumerable<IViewManager> dependencies, IEnumerable<IViewManager> viewManagers, IEventStore eventStore, IDomainEventSerializer domainEventSerializer, IAggregateRootRepository aggregateRootRepository, IDomainTypeNameMapper domainTypeNameMapper, ViewManagerWaitHandle waitHandle, Dictionary<string, object> viewContextItems)
        {
            _dependencies = dependencies.ToList();
            _viewManagers = viewManagers.ToList();
            _eventStore = eventStore;
            _domainEventSerializer = domainEventSerializer;
            _aggregateRootRepository = aggregateRootRepository;
            _domainTypeNameMapper = domainTypeNameMapper;
            _viewContextItems = viewContextItems;
            _workerThread = new Thread(DoWork);

            waitHandle.Register(this);
        }

        void DoWork()
        {
            _logger.Info("Starting dependent view manager event dispatcher (hosting {0}, dependent on {1})",
                string.Join(", ", _viewManagers.Select(v => v.GetType().GetPrettyName())),
                string.Join(", ", _dependencies.Select(v => v.GetType().GetPrettyName())));

            while (_keepWorking)
            {
                Thread.Sleep(100);

                try
                {
                    DoSomeWork();
                }
                catch (Exception exception)
                {
                    _logger.Warn("Could not catch up: {0}", exception);
                    
                    Thread.Sleep(TimeSpan.FromSeconds(2));
                }
            }
        }

        void DoSomeWork()
        {
            if (!_viewManagers.Any()) return;

            var sequenceNumberToCatchUpTo = GetPosition(_dependencies, _eventStore.GetNextGlobalSequenceNumber() - 1);

            var positions = _viewManagers
                .Select(viewManager => new Pos(viewManager, viewManager.GetPosition().Result))
                .ToDictionary(a => a.ViewManager);

            var currentPosition = positions.Min(a => a.Value.Position);

            if (currentPosition >= sequenceNumberToCatchUpTo) return;

            var relevantEventBatches = _eventStore
                .Stream(currentPosition)
                .TakeWhile(e => e.GetGlobalSequenceNumber() <= sequenceNumberToCatchUpTo)
                .Batch(100)
                .Select(b => b.ToList());

            foreach (var e in relevantEventBatches)
            {
                DispatchBatchToViewManagers(_viewManagers, e, positions);
            }
        }

        void DispatchBatchToViewManagers(IEnumerable<IViewManager> viewManagers, IEnumerable<EventData> batch, Dictionary<IViewManager, Pos> positions)
        {
            var context = new DefaultViewContext(_aggregateRootRepository, _domainTypeNameMapper);

            foreach (var kvp in _viewContextItems)
            {
                context.Items[kvp.Key] = kvp.Value;
            }

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
            _workerThread.Start();
        }

        public void Dispatch(IEventStore eventStore, IEnumerable<DomainEvent> events)
        {
        }

        public void Dispose()
        {
            _logger.Info("Stopping dependent view manager event dispatcher");

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
using System;
using System.Collections.Generic;
using System.Diagnostics;
using d60.Cirqus.Aggregates;
using d60.Cirqus.Config.Configurers;
using d60.Cirqus.Events;
using d60.Cirqus.Views;

namespace d60.Cirqus.Diagnostics
{
    public static class DiagnosticsConfigurationExtensions
    {
        /// <summary>
        /// Hooks the given profiler into core operations, allowing for recording relevant execution times
        /// </summary>
        public static void AddProfiler(this OptionsConfigurationBuilder builder, IProfiler profiler)
        {
            var operationProfiler = new OperationProfiler(profiler);

            builder.Decorate<IEventStore>(c => new EventStoreDecorator(c.Get<IEventStore>(), operationProfiler));
            builder.Decorate<IAggregateRootRepository>(c => new AggregateRootRepositoryDecorator(c.Get<IAggregateRootRepository>(), operationProfiler));
            builder.Decorate<IEventDispatcher>(c => new EventDispatcherDecorator(c.Get<IEventDispatcher>(), operationProfiler));
        }

        class OperationProfiler
        {
            readonly IProfiler _profiler;

            public OperationProfiler(IProfiler profiler)
            {
                _profiler = profiler;
            }

            public void RecordAggregateRootGet(TimeSpan elapsed, string aggregateRootId, Type aggregateRootType)
            {
                _profiler.RecordAggregateRootGet(elapsed, aggregateRootType, aggregateRootId);
            }

            public void RecordAggregateRootExists(TimeSpan elapsed, string aggregateRootId)
            {
                _profiler.RecordAggregateRootExists(elapsed, aggregateRootId);
            }

            public void RecordEventBatchSave(TimeSpan elapsed, Guid batchId)
            {
                _profiler.RecordEventBatchSave(elapsed, batchId);
            }

            public void RecordGlobalSequenceNumberGetNext(TimeSpan elapsed)
            {
                _profiler.RecordGlobalSequenceNumberGetNext(elapsed);
            }

            public void RecordEventDispatch(TimeSpan elapsed)
            {
                _profiler.RecordEventDispatch(elapsed);
            }
        }

        class EventDispatcherDecorator : IEventDispatcher
        {
            readonly IEventDispatcher _innerEventDispatcher;
            readonly OperationProfiler _operationProfiler;

            public EventDispatcherDecorator(IEventDispatcher innerEventDispatcher, OperationProfiler operationProfiler)
            {
                _innerEventDispatcher = innerEventDispatcher;
                _operationProfiler = operationProfiler;
            }

            public void Initialize(bool purgeExistingViews = false)
            {
                _innerEventDispatcher.Initialize(purgeExistingViews);
            }

            public void Dispatch(IEnumerable<DomainEvent> events)
            {
                var stopwatch = Stopwatch.StartNew();
                try
                {
                    _innerEventDispatcher.Dispatch(events);
                }
                finally
                {
                    _operationProfiler.RecordEventDispatch(stopwatch.Elapsed);
                }
            }
        }

        class AggregateRootRepositoryDecorator : IAggregateRootRepository
        {
            readonly IAggregateRootRepository _innnerAggregateRootRepository;
            readonly OperationProfiler _operationProfiler;

            public AggregateRootRepositoryDecorator(IAggregateRootRepository innnerAggregateRootRepository, OperationProfiler operationProfiler)
            {
                _innnerAggregateRootRepository = innnerAggregateRootRepository;
                _operationProfiler = operationProfiler;
            }

            public AggregateRoot Get<TAggregateRoot>(string aggregateRootId, IUnitOfWork unitOfWork, long maxGlobalSequenceNumber = long.MaxValue, bool createIfNotExists = false)
            {
                var stopwatch = Stopwatch.StartNew();
                Type actualAggregateRootType = null;

                try
                {
                    var aggregateRoot = _innnerAggregateRootRepository
                        .Get<TAggregateRoot>(aggregateRootId, unitOfWork, maxGlobalSequenceNumber, createIfNotExists);

                    actualAggregateRootType = aggregateRoot.GetType();

                    return aggregateRoot;
                }
                finally
                {
                    _operationProfiler.RecordAggregateRootGet(stopwatch.Elapsed, aggregateRootId, actualAggregateRootType ?? typeof(TAggregateRoot));
                }
            }

            public bool Exists(string aggregateRootId, long maxGlobalSequenceNumber = long.MaxValue)
            {
                var stopwatch = Stopwatch.StartNew();
                try
                {
                    return _innnerAggregateRootRepository
                        .Exists(aggregateRootId, maxGlobalSequenceNumber);
                }
                finally
                {
                    _operationProfiler.RecordAggregateRootExists(stopwatch.Elapsed, aggregateRootId);
                }
            }
        }

        class EventStoreDecorator : IEventStore
        {
            readonly IEventStore _innerEventStore;
            readonly OperationProfiler _operationProfiler;

            public EventStoreDecorator(IEventStore innerEventStore, OperationProfiler operationProfiler)
            {
                _innerEventStore = innerEventStore;
                _operationProfiler = operationProfiler;
            }

            public IEnumerable<EventData> Load(string aggregateRootId, long firstSeq = 0)
            {
                return _innerEventStore.Load(aggregateRootId, firstSeq);
            }

            public IEnumerable<EventData> Stream(long globalSequenceNumber = 0)
            {
                return _innerEventStore.Stream(globalSequenceNumber);
            }

            public long GetNextGlobalSequenceNumber()
            {
                var stopwatch = Stopwatch.StartNew();
                try
                {
                    return _innerEventStore.GetNextGlobalSequenceNumber();
                }
                finally
                {
                    _operationProfiler.RecordGlobalSequenceNumberGetNext(stopwatch.Elapsed);
                }
            }

            public void Save(Guid batchId, IEnumerable<EventData> events)
            {
                var stopwatch = Stopwatch.StartNew();
                try
                {
                    _innerEventStore.Save(batchId, events);
                }
                finally
                {
                    _operationProfiler.RecordEventBatchSave(stopwatch.Elapsed, batchId);
                }
            }
        }
    }
}
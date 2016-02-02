using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using d60.Cirqus.Aggregates;
using d60.Cirqus.Events;
using d60.Cirqus.Extensions;
using d60.Cirqus.Serialization;

namespace d60.Cirqus.Snapshotting.New
{
    class NewSnapshottingAggregateRootRepositoryDecorator : IAggregateRootRepository
    {
        readonly ConcurrentDictionary<Type, bool> _snapshottingEnabledPerRootType = new ConcurrentDictionary<Type, bool>();
        readonly IAggregateRootRepository _aggregateRootRepository;
        readonly IEventStore _eventStore;
        readonly IDomainEventSerializer _domainEventSerializer;
        readonly ISnapshotStore _snapshotStore;
        readonly TimeSpan _preparationThreshold;

        public NewSnapshottingAggregateRootRepositoryDecorator(IAggregateRootRepository aggregateRootRepository, IEventStore eventStore, IDomainEventSerializer domainEventSerializer, ISnapshotStore snapshotStore, TimeSpan preparationThreshold)
        {
            _aggregateRootRepository = aggregateRootRepository;
            _eventStore = eventStore;
            _domainEventSerializer = domainEventSerializer;
            _snapshotStore = snapshotStore;
            _preparationThreshold = preparationThreshold;
        }

        public AggregateRoot Get<TAggregateRoot>(string aggregateRootId, IUnitOfWork unitOfWork, long maxGlobalSequenceNumber = long.MaxValue, bool createIfNotExists = false)
        {
            if (!EnabledFor<TAggregateRoot>())
            {
                return _aggregateRootRepository.Get<TAggregateRoot>(aggregateRootId, unitOfWork, maxGlobalSequenceNumber, createIfNotExists);
            }

            var snapshot = _snapshotStore.LoadSnapshot<TAggregateRoot>(aggregateRootId, maxGlobalSequenceNumber);

            if (snapshot == null)
            {
                var aggregateRootInstance = _aggregateRootRepository.Get<TAggregateRoot>(aggregateRootId, unitOfWork, maxGlobalSequenceNumber, createIfNotExists);
                var checkedOutSequenceNumber = new AggregateRootInfo(aggregateRootInstance).SequenceNumber;
                if (maxGlobalSequenceNumber != long.MaxValue)
                {
                    _snapshotStore.SaveSnapshot<TAggregateRoot>(aggregateRootId, aggregateRootInstance, maxGlobalSequenceNumber);
                }
                OnCommitted<TAggregateRoot>(aggregateRootId, unitOfWork, aggregateRootInstance, checkedOutSequenceNumber);
                return aggregateRootInstance;
            }

            var preparedInstance = PrepareFromSnapshot<TAggregateRoot>(snapshot, maxGlobalSequenceNumber, unitOfWork, aggregateRootId);
            var sequenceNumberOfPreparedInstance = new AggregateRootInfo(preparedInstance).SequenceNumber;
            OnCommitted<TAggregateRoot>(aggregateRootId, unitOfWork, preparedInstance, sequenceNumberOfPreparedInstance);
            return preparedInstance;
        }

        public bool Exists(string aggregateRootId, long maxGlobalSequenceNumber = long.MaxValue)
        {
            return _aggregateRootRepository.Exists(aggregateRootId, maxGlobalSequenceNumber);
        }

        bool EnabledFor<TAggregateRoot>()
        {
            return _snapshottingEnabledPerRootType.GetOrAdd(typeof(TAggregateRoot),
                t => t.GetCustomAttributes(typeof(EnableSnapshotsAttribute), false).Any());
        }

        void OnCommitted<TAggregateRoot>(string aggregateRootId, IUnitOfWork unitOfWork, AggregateRoot aggregateRootInstance, long checkedOutSequenceNumber)
        {
            unitOfWork.Committed += eventBatch =>
            {
                var aggregateRootHasChanges = new AggregateRootInfo(aggregateRootInstance).SequenceNumber != checkedOutSequenceNumber;

                if (!aggregateRootHasChanges) return;

                var lastGlobalSequenceNumber = eventBatch.Max(e => e.GetGlobalSequenceNumber());
                _snapshotStore.SaveSnapshot<TAggregateRoot>(aggregateRootId, aggregateRootInstance, lastGlobalSequenceNumber);
            };
        }

        AggregateRoot PrepareFromSnapshot<TAggregateRoot>(Snapshot matchingSnapshot, long maxGlobalSequenceNumber, IUnitOfWork unitOfWork, string aggregateRootId)
        {
            var instance = matchingSnapshot.Instance;
            var hadEventsAppliedToIt = false;
            var stopwatch = Stopwatch.StartNew();

            if (matchingSnapshot.ValidFromGlobalSequenceNumber < maxGlobalSequenceNumber)
            {
                var info = new AggregateRootInfo(instance);

                foreach (var e in _eventStore.Load(aggregateRootId, info.SequenceNumber + 1))
                {
                    if (e.GetGlobalSequenceNumber() > maxGlobalSequenceNumber) break;
                    var domainEvent = _domainEventSerializer.Deserialize(e);
                    info.Apply(domainEvent, unitOfWork);
                    hadEventsAppliedToIt = true;
                }

                info.SetUnitOfWork(unitOfWork);
            }

            if (hadEventsAppliedToIt && stopwatch.Elapsed > _preparationThreshold)
            {
                _snapshotStore.SaveSnapshot<TAggregateRoot>(aggregateRootId, instance, maxGlobalSequenceNumber);
            }

            return instance;
        }
    }
}
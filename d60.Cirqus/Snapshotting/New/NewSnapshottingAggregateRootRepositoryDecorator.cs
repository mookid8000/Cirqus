using System.Linq;
using d60.Cirqus.Aggregates;
using d60.Cirqus.Events;
using d60.Cirqus.Extensions;
using d60.Cirqus.Serialization;

namespace d60.Cirqus.Snapshotting.New
{
    public class NewSnapshottingAggregateRootRepositoryDecorator : IAggregateRootRepository
    {
        readonly IAggregateRootRepository _aggregateRootRepository;
        readonly IEventStore _eventStore;
        readonly IDomainEventSerializer _domainEventSerializer;
        readonly ISnapshotStore _snapshotStore;

        public NewSnapshottingAggregateRootRepositoryDecorator(IAggregateRootRepository aggregateRootRepository, IEventStore eventStore, IDomainEventSerializer domainEventSerializer, ISnapshotStore snapshotStore)
        {
            _aggregateRootRepository = aggregateRootRepository;
            _eventStore = eventStore;
            _domainEventSerializer = domainEventSerializer;
            _snapshotStore = snapshotStore;
        }

        public AggregateRoot Get<TAggregateRoot>(string aggregateRootId, IUnitOfWork unitOfWork, long maxGlobalSequenceNumber = long.MaxValue, bool createIfNotExists = false)
        {
            if (!_snapshotStore.EnabledFor<TAggregateRoot>())
            {
                return _aggregateRootRepository.Get<TAggregateRoot>(aggregateRootId, unitOfWork, maxGlobalSequenceNumber, createIfNotExists);
            }

            var snapshot = _snapshotStore.TryGetSnapshot<TAggregateRoot>(aggregateRootId, maxGlobalSequenceNumber);

            if (snapshot == null)
            {
                var aggregateRootInstance = _aggregateRootRepository.Get<TAggregateRoot>(aggregateRootId, unitOfWork, maxGlobalSequenceNumber, createIfNotExists);
                var checkedOutSequenceNumber = new AggregateRootInfo(aggregateRootInstance).SequenceNumber;
                if (maxGlobalSequenceNumber != long.MaxValue)
                {
                    _snapshotStore.SaveSnapshot<TAggregateRoot>(aggregateRootId, aggregateRootInstance, checkedOutSequenceNumber, false, maxGlobalSequenceNumber);
                }
                OnCommitted<TAggregateRoot>(aggregateRootId, unitOfWork, aggregateRootInstance, checkedOutSequenceNumber);
                return aggregateRootInstance;
            }

            var preparedInstance = PrepareFromSnapshot(snapshot, maxGlobalSequenceNumber, unitOfWork);
            var sequenceNumberOfPreparedInstance = new AggregateRootInfo(preparedInstance).SequenceNumber;
            OnCommitted<TAggregateRoot>(aggregateRootId, unitOfWork, preparedInstance, sequenceNumberOfPreparedInstance);
            return preparedInstance;
        }

        void OnCommitted<TAggregateRoot>(string aggregateRootId, IUnitOfWork unitOfWork, AggregateRoot aggregateRootInstance, long checkedOutSequenceNumber)
        {
            unitOfWork.Committed += eventBatch =>
            {
                var batch = eventBatch.ToList();
                if (!batch.Any()) return;

                var lastGlobalSequenceNumber = batch.Max(e => e.GetGlobalSequenceNumber());

                _snapshotStore.SaveSnapshot<TAggregateRoot>(aggregateRootId, aggregateRootInstance, checkedOutSequenceNumber, true, lastGlobalSequenceNumber);
            };
        }

        AggregateRoot PrepareFromSnapshot(Snapshot matchingSnapshot, long maxGlobalSequenceNumber, IUnitOfWork unitOfWork)
        {
            var instance = matchingSnapshot.Instance;

            if (matchingSnapshot.ValidFromGlobalSequenceNumber < maxGlobalSequenceNumber)
            {
                var info = new AggregateRootInfo(instance);

                foreach (var e in _eventStore.Load(matchingSnapshot.AggregateRootId, info.SequenceNumber + 1))
                {
                    if (e.GetGlobalSequenceNumber() > maxGlobalSequenceNumber) break;
                    var domainEvent = _domainEventSerializer.Deserialize(e);
                    info.Apply(domainEvent, unitOfWork);
                }

                info.SetUnitOfWork(unitOfWork);
            }

            return instance;
        }

        public bool Exists(string aggregateRootId, long maxGlobalSequenceNumber = long.MaxValue)
        {
            return _aggregateRootRepository.Exists(aggregateRootId, maxGlobalSequenceNumber);
        }
    }
}
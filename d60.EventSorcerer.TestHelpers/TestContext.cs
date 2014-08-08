using System;
using System.Collections.Generic;
using System.Linq;
using d60.EventSorcerer.Aggregates;
using d60.EventSorcerer.Events;
using d60.EventSorcerer.Extensions;
using d60.EventSorcerer.Numbers;
using d60.EventSorcerer.Serialization;
using d60.EventSorcerer.TestHelpers.Internals;
using d60.EventSorcerer.Views.Basic;

namespace d60.EventSorcerer.TestHelpers
{
    /// <summary>
    /// Use this bad boy to test your CQRS+ES things
    /// </summary>
    public class TestContext
    {
        readonly Serializer _serializer = new Serializer("<events>");
        readonly InMemoryUnitOfWork _unitOfWork = new InMemoryUnitOfWork();
        readonly InMemoryEventStore _eventStore = new InMemoryEventStore();
        readonly DefaultAggregateRootRepository _aggregateRootRepository;
        readonly BasicEventDispatcher _eventDispatcher;
        DateTime _currentTime = DateTime.MinValue;
        bool _initialized;

        public TestContext()
        {
            _aggregateRootRepository = new DefaultAggregateRootRepository(_eventStore);
            _eventDispatcher = new BasicEventDispatcher(_aggregateRootRepository);
        }

        public TestContext AddViewManager(IViewManager viewManager)
        {
            _eventDispatcher.Add(viewManager);
            return this;
        }

        public IEnumerable<AggregateRootTestInfo> AggregateRootsInHistory
        {
            get
            {
                return _eventStore.GroupBy(e => e.GetAggregateRootId())
                    .Select(group => new AggregateRootTestInfo(group.Key, group.Max(g => g.GetSequenceNumber()), group.Max(g => g.GetGlobalSequenceNumber())));
            }
        }

        public void SetCurrentTime(DateTime fixedCurrentTime)
        {
            _currentTime = fixedCurrentTime;
        }

        public TAggregateRoot Get<TAggregateRoot>(Guid aggregateRootId) where TAggregateRoot : AggregateRoot, new()
        {
            var aggregateRootFromCache = _unitOfWork.GetAggregateRootFromCache<TAggregateRoot>(aggregateRootId, long.MaxValue);
            if (aggregateRootFromCache != null)
            {
                return aggregateRootFromCache;
            }

            var aggregateRootInfo = _aggregateRootRepository.Get<TAggregateRoot>(aggregateRootId, _unitOfWork);
            var aggregateRoot = aggregateRootInfo.AggregateRoot;

            _unitOfWork.AddToCache(aggregateRoot, long.MaxValue);

            aggregateRoot.UnitOfWork = _unitOfWork;
            aggregateRoot.SequenceNumberGenerator = new CachingSequenceNumberGenerator(aggregateRootInfo.LastSeqNo + 1);

            _unitOfWork.AddToCache(aggregateRoot, aggregateRootInfo.LastGlobalSeqNo);

            if (aggregateRootInfo.IsNew)
            {
                aggregateRoot.InvokeCreated();
            }

            return aggregateRoot;
        }

        public void Initialize()
        {
            EnsureInitialized();
        }

        /// <summary>
        /// Commits the current unit of work (i.e. emitted events from <see cref="UnitOfWork"/> are saved
        /// to the history and will be used to hydrate aggregate roots from now on.
        /// </summary>
        public void Commit()
        {
            EnsureInitialized();

            var domainEvents = UnitOfWork.ToList();

            if (!domainEvents.Any()) return;

            _eventStore.Save(Guid.NewGuid(), domainEvents);

            _unitOfWork.Clear();

            _eventDispatcher.Dispatch(_eventStore, domainEvents);
        }

        void EnsureInitialized()
        {
            if (!_initialized)
            {
                _eventDispatcher.Initialize(_eventStore, purgeExistingViews: true);
                _initialized = true;
            }
        }

        /// <summary>
        /// Saves the given domain event to the history - requires that the aggregate root ID has been added in the event's metadata under the <see cref="DomainEvent.MetadataKeys.AggregateRootId"/> key
        /// </summary>
        public void Save<TAggregateRoot>(DomainEvent<TAggregateRoot> domainEvent) where TAggregateRoot : AggregateRoot
        {
            if (!domainEvent.Meta.ContainsKey(DomainEvent.MetadataKeys.AggregateRootId))
            {
                throw new InvalidOperationException(
                    string.Format(
                        "Canno save domain event {0} because it does not have an aggregate root ID! Use the Save(id, event) overload or make sure that the '{1}' metadata key has been set",
                        domainEvent, DomainEvent.MetadataKeys.AggregateRootId));
            }

            Save(domainEvent.GetAggregateRootId(), domainEvent);
        }

        /// <summary>
        /// Saves the given domain event to the history as if it was emitted by the specified aggregate root
        /// </summary>
        public void Save<TAggregateRoot>(Guid aggregateRootId, DomainEvent<TAggregateRoot> domainEvent) where TAggregateRoot : AggregateRoot
        {
            var now = GetNow();

            domainEvent.Meta[DomainEvent.MetadataKeys.AggregateRootId] = aggregateRootId;
            domainEvent.Meta[DomainEvent.MetadataKeys.SequenceNumber] = _eventStore.GetNextSeqNo(aggregateRootId);
            domainEvent.Meta[DomainEvent.MetadataKeys.Owner] = AggregateRoot.GetOwnerFromType(typeof(TAggregateRoot));
            domainEvent.Meta[DomainEvent.MetadataKeys.TimeLocal] = now.ToLocalTime();
            domainEvent.Meta[DomainEvent.MetadataKeys.TimeUtc] = now;

            domainEvent.Meta.TakeFromAttributes(domainEvent.GetType());
            domainEvent.Meta.TakeFromAttributes(typeof(TAggregateRoot));

            _serializer.EnsureSerializability(domainEvent);

            _eventStore.Save(Guid.NewGuid(), new[] { domainEvent });
        }


        DateTime GetNow()
        {
            if (_currentTime == DateTime.MinValue)
            {
                return DateTime.UtcNow;
            }

            var timeToReturn = _currentTime.ToUniversalTime();
            
            // simulate time progressing
            _currentTime = _currentTime.AddTicks(1);

            return timeToReturn;
        }

        /// <summary>
        /// Gets the events collected in the current unit of work
        /// </summary>
        public IEnumerable<DomainEvent> UnitOfWork
        {
            get { return _unitOfWork.ToList(); }
        }

        /// <summary>
        /// Gets the entire history of commited events from the event store
        /// </summary>
        public IEnumerable<DomainEvent> History
        {
            get { return _eventStore.Stream().ToList(); }
        }
    }

    public class AggregateRootTestInfo  
    {
        public AggregateRootTestInfo(Guid id, long seqNo, long globalSeqNo)
        {
            Id = id;
            SeqNo = seqNo;
            GlobalSeqNo = globalSeqNo;
        }

        public Guid Id { get; private set; }
        
        public long SeqNo { get; private set; }
        
        public long GlobalSeqNo { get; private set; }
        public override string ToString()
        
        {
            return string.Format("{0}: {1} ({2})", Id, SeqNo, GlobalSeqNo);
        }
    }
}
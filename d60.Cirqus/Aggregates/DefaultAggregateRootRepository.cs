using System;
using System.Linq;
using d60.Cirqus.Config;
using d60.Cirqus.Events;
using d60.Cirqus.Extensions;
using d60.Cirqus.Serialization;

namespace d60.Cirqus.Aggregates
{
    /// <summary>
    /// Standard replaying aggregate root repository that will return an aggregate root and always replay all events in order to bring it up-to-date
    /// </summary>
    public class DefaultAggregateRootRepository : IAggregateRootRepository
    {
        readonly IEventStore _eventStore;
        readonly IDomainEventSerializer _domainEventSerializer;
        readonly IDomainTypeNameMapper _domainTypeNameMapper;

        public DefaultAggregateRootRepository(IEventStore eventStore, IDomainEventSerializer domainEventSerializer, IDomainTypeNameMapper domainTypeNameMapper)
        {
            _eventStore = eventStore;
            _domainEventSerializer = domainEventSerializer;
            _domainTypeNameMapper = domainTypeNameMapper;
        }

        /// <summary>
        /// Checks whether one or more events exist for an aggregate root with the specified ID. If <seealso cref="maxGlobalSequenceNumber"/> is specified,
        /// it will check whether the root instance existed at that point in time
        /// </summary>
        public bool Exists(string aggregateRootId, long maxGlobalSequenceNumber = long.MaxValue, IUnitOfWork unitOfWork = null)
        {
            var firstEvent = _eventStore.Load(aggregateRootId).FirstOrDefault();

            return firstEvent != null && firstEvent.GetGlobalSequenceNumber() <= maxGlobalSequenceNumber;
        }

        /// <summary>
        /// Gets the aggregate root of the specified type with the specified ID by hydrating it with events from the event store. The
        /// root will have events replayed until the specified <seealso cref="maxGlobalSequenceNumber"/> ceiling. If the root has
        /// no events (i.e. it doesn't exist yet), a newly initialized instance is returned.
        /// </summary>
        public AggregateRoot Get<TAggregateRoot>(string aggregateRootId, IUnitOfWork unitOfWork, long maxGlobalSequenceNumber = long.MaxValue, bool createIfNotExists = false)
        {
            var domainEventsForThisAggregate = _eventStore.Load(aggregateRootId);

            var eventsToApply = domainEventsForThisAggregate
                .Where(e => e.GetGlobalSequenceNumber() <= maxGlobalSequenceNumber)
                .Select(e => _domainEventSerializer.Deserialize(e));

            AggregateRoot aggregateRoot = null;

            foreach (var e in eventsToApply)
            {
                if (aggregateRoot == null)
                {
                    var aggregateRootType = _domainTypeNameMapper.GetType(e.Meta[DomainEvent.MetadataKeys.Owner]);
                    aggregateRoot = CreateNewAggregateRootInstance(aggregateRootType, aggregateRootId, unitOfWork);
                }

                aggregateRoot.ApplyEvent(e);
            }

            if (aggregateRoot == null)
            {
                aggregateRoot = CreateNewAggregateRootInstance(typeof(TAggregateRoot), aggregateRootId, unitOfWork);

                aggregateRoot.InvokeCreated();
            }

            return aggregateRoot;
        }

        AggregateRoot CreateNewAggregateRootInstance(Type aggregateRootType, string aggregateRootId, IUnitOfWork unitOfWork)
        {
            var aggregateRoot = (AggregateRoot)Activator.CreateInstance(aggregateRootType);
            
            aggregateRoot.Initialize(aggregateRootId);
            aggregateRoot.UnitOfWork = unitOfWork;

            return aggregateRoot;
        }
    }
}
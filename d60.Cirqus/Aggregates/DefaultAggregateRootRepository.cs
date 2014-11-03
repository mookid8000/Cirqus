using System;
using System.Linq;
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

        public DefaultAggregateRootRepository(IEventStore eventStore, IDomainEventSerializer domainEventSerializer)
        {
            _eventStore = eventStore;
            _domainEventSerializer = domainEventSerializer;
        }

        /// <summary>
        /// Checks whether one or more events exist for an aggregate root with the specified ID. If <seealso cref="maxGlobalSequenceNumber"/> is specified,
        /// it will check whether the root instance existed at that point in time
        /// </summary>
        public bool Exists<TAggregateRoot>(string aggregateRootId, long maxGlobalSequenceNumber = long.MaxValue, IUnitOfWork unitOfWork = null) 
            where TAggregateRoot : AggregateRoot
        {
            var firstEvent = _eventStore.Load(aggregateRootId).FirstOrDefault();

            return firstEvent != null && firstEvent.GetGlobalSequenceNumber() <= maxGlobalSequenceNumber;
        }

        /// <summary>
        /// Gets the aggregate root of the specified type with the specified ID by hydrating it with events from the event store. The
        /// root will have events replayed until the specified <seealso cref="maxGlobalSequenceNumber"/> ceiling. If the root has
        /// no events (i.e. it doesn't exist yet), a newly initialized instance is returned.
        /// </summary>
        public AggregateRootInfo<TAggregateRoot> Get<TAggregateRoot>(string aggregateRootId, IUnitOfWork unitOfWork, long maxGlobalSequenceNumber = long.MaxValue, bool createIfNotExists = false) 
            where TAggregateRoot : AggregateRoot, new()
        {
            var aggregateRootInfo = CreateNewAggregateRootInstance<TAggregateRoot>(aggregateRootId);
            var domainEventsForThisAggregate = _eventStore.Load(aggregateRootId);

            var eventsToApply = domainEventsForThisAggregate
                .Where(e => e.GetGlobalSequenceNumber() <= maxGlobalSequenceNumber)
                .Select(e => _domainEventSerializer.Deserialize(e));

            aggregateRootInfo.Apply(eventsToApply, unitOfWork);

            if (aggregateRootInfo.IsNew && !createIfNotExists)
            {
                throw new ArgumentException(string.Format("Could not find aggregate root of type {0} with ID {1}", typeof(TAggregateRoot), aggregateRootId));
            }

            return aggregateRootInfo;
        }

        AggregateRootInfo<TAggregateRoot> CreateNewAggregateRootInstance<TAggregateRoot>(string aggregateRootId) where TAggregateRoot : AggregateRoot, new()
        {
            var aggregate = new TAggregateRoot();
            
            aggregate.Initialize(aggregateRootId);

            return AggregateRootInfo<TAggregateRoot>.Create(aggregate);
        }
    }
}
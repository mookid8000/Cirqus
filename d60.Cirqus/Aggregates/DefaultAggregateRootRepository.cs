using System;
using System.Linq;
using d60.Cirqus.Events;
using d60.Cirqus.Extensions;

namespace d60.Cirqus.Aggregates
{
    /// <summary>
    /// Standard replaying aggregate root repository that will return an aggregate root and always replay all events in order to bring it up-to-date
    /// </summary>
    public class DefaultAggregateRootRepository : IAggregateRootRepository
    {
        readonly IEventStore _eventStore;

        public DefaultAggregateRootRepository(IEventStore eventStore)
        {
            _eventStore = eventStore;
        }

        /// <summary>
        /// Checks whether one or more events exist for an aggregate root with the specified ID. If <seealso cref="maxGlobalSequenceNumber"/> is specified,
        /// it will check whether the root instance existed at that point in time
        /// </summary>
        public bool Exists<TAggregate>(Guid aggregateRootId, long maxGlobalSequenceNumber = long.MaxValue, IUnitOfWork unitOfWork = null) 
            where TAggregate : AggregateRoot
        {
            var firstEvent = _eventStore.Load(aggregateRootId).FirstOrDefault();

            return firstEvent != null && firstEvent.GetGlobalSequenceNumber() <= maxGlobalSequenceNumber;
        }

        /// <summary>
        /// Gets the aggregate root of the specified type with the specified ID by hydrating it with events from the event store. The
        /// root will have events replayed until the specified <seealso cref="maxGlobalSequenceNumber"/> ceiling. If the root has
        /// no events (i.e. it doesn't exist yet), a newly initialized instance is returned.
        /// </summary>
        public AggregateRootInfo<TAggregate> Get<TAggregate>(Guid aggregateRootId, IUnitOfWork unitOfWork, long maxGlobalSequenceNumber = long.MaxValue) 
            where TAggregate : AggregateRoot, new()
        {
            var aggregateRootInfo = CreateNewAggregateRootInstance<TAggregate>(aggregateRootId);
            var domainEventsForThisAggregate = _eventStore.Load(aggregateRootId);

            var eventsToApply = domainEventsForThisAggregate
                .Where(e => e.GetGlobalSequenceNumber() <= maxGlobalSequenceNumber);

            aggregateRootInfo.Apply(eventsToApply, unitOfWork);

            return aggregateRootInfo;
        }

        AggregateRootInfo<TAggregateRoot> CreateNewAggregateRootInstance<TAggregateRoot>(Guid aggregateRootId) where TAggregateRoot : AggregateRoot, new()
        {
            var aggregate = new TAggregateRoot();
            
            aggregate.Initialize(aggregateRootId);

            return AggregateRootInfo<TAggregateRoot>.Create(aggregate);
        }
    }
}
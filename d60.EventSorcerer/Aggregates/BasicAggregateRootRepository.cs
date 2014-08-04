using System;
using System.Collections.Generic;
using System.Linq;
using d60.EventSorcerer.Events;

namespace d60.EventSorcerer.Aggregates
{
    /// <summary>
    /// Basic aggregate root repository that will return an aggregate root and always replay all events in order to bring it up-to-date
    /// </summary>
    public class BasicAggregateRootRepository : IAggregateRootRepository
    {
        readonly IEventStore _eventStore;

        public BasicAggregateRootRepository(IEventStore eventStore)
        {
            _eventStore = eventStore;
        }

        public TAggregate Get<TAggregate>(Guid aggregateRootId, long maxGlobalSequenceNumber = int.MaxValue) where TAggregate : AggregateRoot, new()
        {
            var aggregate = CreateFreshAggregate<TAggregate>(aggregateRootId);
            var domainEventsForThisAggregate = _eventStore.Load(aggregateRootId);

            ApplyEvents(aggregate, domainEventsForThisAggregate);

            return aggregate;
        }

        public bool Exists<TAggregate>(Guid aggregateRootId) where TAggregate : AggregateRoot
        {
            var firstEvent = _eventStore.Load(aggregateRootId, 0, 1).FirstOrDefault();

            return firstEvent != null
                   && firstEvent.Meta.ContainsKey(DomainEvent.MetadataKeys.Owner)
                   && firstEvent.Meta[DomainEvent.MetadataKeys.Owner].ToString() == AggregateRoot.GetOwnerFromType(typeof(TAggregate));
        }

        TAggregate CreateFreshAggregate<TAggregate>(Guid aggregateRootId) where TAggregate : AggregateRoot, new()
        {
            var aggregate = new TAggregate();
            
            aggregate.Initialize(aggregateRootId, this);
            
            return aggregate;
        }

        void ApplyEvents<TAggregate>(TAggregate aggregate, IEnumerable<DomainEvent> domainEventsForThisAggregate)
            where TAggregate : AggregateRoot, new()
        {
            var dynamicAggregate = (dynamic) aggregate;

            foreach (var e in domainEventsForThisAggregate)
            {
                dynamicAggregate.Apply((dynamic) e);
            }
        }
    }
}
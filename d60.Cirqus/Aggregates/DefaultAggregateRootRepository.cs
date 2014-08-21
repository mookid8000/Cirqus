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

        public bool Exists<TAggregate>(Guid aggregateRootId, long maxGlobalSequenceNumber = long.MaxValue, IUnitOfWork unitOfWork = null) where TAggregate : AggregateRoot
        {
            var firstEvent = _eventStore.Load(aggregateRootId, 0, 1).FirstOrDefault();

            return firstEvent != null && firstEvent.GetGlobalSequenceNumber() <= maxGlobalSequenceNumber;
        }

        public AggregateRootInfo<TAggregate> Get<TAggregate>(Guid aggregateRootId, IUnitOfWork unitOfWork, long maxGlobalSequenceNumber = long.MaxValue) where TAggregate : AggregateRoot, new()
        {
            var aggregateRootInfo = CreateFreshAggregate<TAggregate>(aggregateRootId);
            var domainEventsForThisAggregate = _eventStore.Load(aggregateRootId);

            var eventsToApply = domainEventsForThisAggregate
                .Where(e => e.GetGlobalSequenceNumber() <= maxGlobalSequenceNumber);

            aggregateRootInfo.Apply(eventsToApply, unitOfWork);

            return aggregateRootInfo;
        }

        AggregateRootInfo<TAggregateRoot> CreateFreshAggregate<TAggregateRoot>(Guid aggregateRootId) where TAggregateRoot : AggregateRoot, new()
        {
            var aggregate = new TAggregateRoot();
            
            aggregate.Initialize(aggregateRootId);
            aggregate.AggregateRootRepository = this;
            
            return AggregateRootInfo<TAggregateRoot>.New(aggregate);
        }
    }
}
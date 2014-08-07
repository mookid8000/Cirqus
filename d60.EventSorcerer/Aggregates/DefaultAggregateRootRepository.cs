using System;
using System.Collections.Generic;
using System.Linq;
using d60.EventSorcerer.Events;
using d60.EventSorcerer.Extensions;

namespace d60.EventSorcerer.Aggregates
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

        public AggregateRootInfo<TAggregate> Get<TAggregate>(Guid aggregateRootId, long maxGlobalSequenceNumber = long.MaxValue) where TAggregate : AggregateRoot, new()
        {
            var aggregate = CreateFreshAggregate<TAggregate>(aggregateRootId);
            var domainEventsForThisAggregate = _eventStore.Load(aggregateRootId);

            var eventsToApply = domainEventsForThisAggregate
                .Where(e => e.GetGlobalSequenceNumber() <= maxGlobalSequenceNumber)
                .ToList();

            ApplyEvents(aggregate, eventsToApply);

            if (!eventsToApply.Any())
            {
                return AggregateRootInfo<TAggregate>.New(aggregate);
            }
            
            var last = eventsToApply.Last();

            return AggregateRootInfo<TAggregate>.Old(aggregate, last.GetSequenceNumber(), last.GetGlobalSequenceNumber());
        }

        public bool Exists<TAggregate>(Guid aggregateRootId, long maxGlobalSequenceNumber = long.MaxValue) where TAggregate : AggregateRoot
        {
            var firstEvent = _eventStore.Load(aggregateRootId, 0, 1).FirstOrDefault();

            return firstEvent != null && firstEvent.GetGlobalSequenceNumber() <= maxGlobalSequenceNumber;
        }

        TAggregate CreateFreshAggregate<TAggregate>(Guid aggregateRootId) where TAggregate : AggregateRoot, new()
        {
            var aggregate = new TAggregate();
            
            aggregate.Initialize(aggregateRootId);
            aggregate.AggregateRootRepository = this;
            
            return aggregate;
        }

        void ApplyEvents<TAggregate>(TAggregate aggregate, IEnumerable<DomainEvent> domainEventsForThisAggregate)
            where TAggregate : AggregateRoot, new()
        {
            var dynamicAggregate = (dynamic) aggregate;

            using (new ThrowingEventCollector(aggregate))
            {
                foreach (var e in domainEventsForThisAggregate)
                {
                    aggregate.GlobalSequenceNumberCutoff = e.GetGlobalSequenceNumber();

                    try
                    {
                        dynamicAggregate.Apply((dynamic) e);
                    }
                    catch (Exception exception)
                    {
                        throw new ApplicationException(string.Format("Could not apply event {0} to {1}", e, aggregate),
                            exception);
                    }
                }
            }
        }

        /// <summary>
        /// Sensitive <see cref="IEventCollector"/> stub that can be mounted on an aggregate root when it is in a state
        /// where it is NOT allowed to emit events.
        /// </summary>
        class ThrowingEventCollector : IEventCollector, IDisposable
        {
            readonly AggregateRoot _root;
            readonly IEventCollector _originalEventCollector;

            public ThrowingEventCollector(AggregateRoot root)
            {
                _root = root;
                _originalEventCollector = _root.EventCollector;
                _root.EventCollector = this;
            }

            public void Add(DomainEvent e)
            {
                throw new InvalidOperationException(string.Format("The aggregate root of type {0} with ID {1} attempted to emit event {2} while applying events, which is not allowed",
                    _root.GetType(), _root.Id, e));
            }

            public void Dispose()
            {
                _root.EventCollector = _originalEventCollector;
            }
        }
    }
}
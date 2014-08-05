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

            using (new ThrowingEventCollector(aggregate))
            {
                foreach (var e in domainEventsForThisAggregate)
                {
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
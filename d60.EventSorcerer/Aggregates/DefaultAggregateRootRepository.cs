using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
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

        public AggregateRootInfo<TAggregate> Get<TAggregate>(Guid aggregateRootId, IUnitOfWork unitOfWork, long maxGlobalSequenceNumber = long.MaxValue) where TAggregate : AggregateRoot, new()
        {
            var aggregate = CreateFreshAggregate<TAggregate>(aggregateRootId);
            var domainEventsForThisAggregate = _eventStore.Load(aggregateRootId);

            var eventsToApply = domainEventsForThisAggregate
                .Where(e => e.GetGlobalSequenceNumber() <= maxGlobalSequenceNumber)
                .ToList();

            aggregate.UnitOfWork = unitOfWork;

            ApplyEvents(aggregate, eventsToApply);

            if (!eventsToApply.Any())
            {
                return AggregateRootInfo<TAggregate>.New(aggregate);
            }
            
            var last = eventsToApply.Last();

            return AggregateRootInfo<TAggregate>.Old(aggregate, last.GetSequenceNumber(), last.GetGlobalSequenceNumber());
        }

        public bool Exists<TAggregate>(Guid aggregateRootId, long maxGlobalSequenceNumber = long.MaxValue, IUnitOfWork unitOfWork = null) where TAggregate : AggregateRoot
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
            var previousCutoff = aggregate.GlobalSequenceNumberCutoff;

            using (new ThrowingUnitOfWork(aggregate))
            {
                foreach (var e in domainEventsForThisAggregate)
                {
                    // ensure that other aggregates loaded during event application are historic if that's required
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

            // restore the cutoff so we don't hinder the root's ability to load other aggregate roots from its emitter methods
            aggregate.GlobalSequenceNumberCutoff = previousCutoff;
        }

        /// <summary>
        /// Sensitive <see cref="IUnitOfWork"/> stub that can be mounted on an aggregate root when it is in a state
        /// where it is NOT allowed to emit events.
        /// </summary>
        class ThrowingUnitOfWork : IUnitOfWork, IDisposable
        {
            readonly AggregateRoot _root;
            readonly IUnitOfWork _originalUnitOfWork;

            public ThrowingUnitOfWork(AggregateRoot root)
            {
                _root = root;
                _originalUnitOfWork = _root.UnitOfWork;
                _root.UnitOfWork = this;
            }

            public void AddEmittedEvent(DomainEvent e)
            {
                throw new InvalidOperationException(string.Format("The aggregate root of type {0} with ID {1} attempted to emit event {2} while applying events, which is not allowed",
                    _root.GetType(), _root.Id, e));
            }

            public TAggregateRoot GetAggregateRootFromCache<TAggregateRoot>(Guid aggregateRootId, long globalSequenceNumberCutoff) where TAggregateRoot : AggregateRoot
            {
                return _originalUnitOfWork.GetAggregateRootFromCache<TAggregateRoot>(aggregateRootId, globalSequenceNumberCutoff);
            }

            public void AddToCache<TAggregateRoot>(TAggregateRoot aggregateRoot, long globalSequenceNumberCutoff) where TAggregateRoot : AggregateRoot
            {
                _originalUnitOfWork.AddToCache(aggregateRoot, globalSequenceNumberCutoff);
            }

            public void Dispose()
            {
                _root.UnitOfWork = _originalUnitOfWork;
            }
        }
    }
}
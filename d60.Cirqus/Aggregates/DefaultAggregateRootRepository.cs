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
        public bool Exists(string aggregateRootId, long maxGlobalSequenceNumber = long.MaxValue)
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
                    if (!e.Meta.ContainsKey(DomainEvent.MetadataKeys.Owner))
                    {
                        throw new InvalidOperationException(string.Format("Attempted to load aggregate root with ID {0} but the first event {1} did not contain metadata with the aggregate root type name!",
                            aggregateRootId, e));
                    }

                    var aggregateRootTypeName = e.Meta[DomainEvent.MetadataKeys.Owner];
                    var aggregateRootType = _domainTypeNameMapper.GetType(aggregateRootTypeName);
                    aggregateRoot = CreateNewAggregateRootInstance(aggregateRootType, aggregateRootId, unitOfWork);
                }

                aggregateRoot.ApplyEvent(e, ReplayState.ReplayApply);
            }

            if (aggregateRoot == null)
            {
                if (!createIfNotExists)
                {
                    throw new ArgumentException(string.Format("Attempted to load aggregate root with ID {0} as {1}, but it didn't exist!", aggregateRootId, typeof(TAggregateRoot)));
                }

                aggregateRoot = CreateNewAggregateRootInstance(typeof(TAggregateRoot), aggregateRootId, unitOfWork);
            }

            return aggregateRoot;
        }

        /// <summary>
        /// Inheritors should override this to create instances of the specified type.
        /// </summary>
        /// <param name="aggregateRootType">The type of the aggregate root to create - already validated to ensure it is a sub-type of <see cref="AggregateRoot"/>.</param>
        /// <returns>An instance of <paramref name="aggregateRootType"/>.</returns>
        protected virtual AggregateRoot CreateAggregateRootInstance(Type aggregateRootType)
        {
            return (AggregateRoot)Activator.CreateInstance(aggregateRootType);
        }

        AggregateRoot CreateNewAggregateRootInstance(Type aggregateRootType, string aggregateRootId, IUnitOfWork unitOfWork)
        {
            if (!typeof (AggregateRoot).IsAssignableFrom(aggregateRootType))
            {
                throw new ArgumentException(string.Format("Cannot create new aggregate root with ID {0} of type {1} because it is not derived from AggregateRoot!",
                    aggregateRootId, aggregateRootType));
            }

            var aggregateRoot = CreateAggregateRootInstance(aggregateRootType);
            
            aggregateRoot.Initialize(aggregateRootId);
            aggregateRoot.UnitOfWork = unitOfWork;
            
            return aggregateRoot;
        }
    }
}
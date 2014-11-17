using d60.Cirqus.Aggregates;

namespace d60.Cirqus.Events
{
    /// <summary>
    /// A thing that is capable of collecting emitted events
    /// </summary>
    public interface IUnitOfWork
    {
        /// <summary>
        /// Adds an emitted event to the current unit of work, staging it for being committed with the next event batch
        /// </summary>
        void AddEmittedEvent<TAggregateRoot>(DomainEvent<TAggregateRoot> e) where TAggregateRoot : AggregateRoot;

        /// <summary>
        /// Adds the given aggregate root to the unit of work, ensuring that the instance is reused if it gets loaded again
        /// </summary>
        void AddToCache(AggregateRoot aggregateRoot, long globalSequenceNumberCutoff);

        /// <summary>
        /// Checks whether an aggregate root with the given ID exists (i.e is there more than 0 events for the aggregate root with that ID)
        /// </summary>
        bool Exists(string aggregateRootId, long globalSequenceNumberCutoff);

        /// <summary>
        /// Gets from the cache or from another relevant place the aggregate root instance with the given ID
        /// </summary>
        AggregateRoot Get(string aggregateRootId, long globalSequenceNumberCutoff, bool createIfNotExists = false);
    }
}
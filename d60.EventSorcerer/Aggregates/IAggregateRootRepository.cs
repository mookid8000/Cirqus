using System;

namespace d60.EventSorcerer.Aggregates
{
    /// <summary>
    /// Repository of aggregate roots.
    /// </summary>
    public interface IAggregateRootRepository
    {
        /// <summary>
        /// Returns a fully hydrated and ready to use aggregate root instance of the specified type
        /// </summary>
        TAggregate Get<TAggregate>(Guid aggregateRootId) where TAggregate : AggregateRoot, new();

        /// <summary>
        /// Checks whether an aggregate root of the specified type with the specified ID exists
        /// </summary>
        bool Exists<TAggregate>(Guid aggregateRootId) where TAggregate : AggregateRoot;
    }
}
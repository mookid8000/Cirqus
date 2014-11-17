using System;
using d60.Cirqus.Aggregates;

namespace d60.Cirqus.Commands
{
    /// <summary>
    /// Provides a context for the execution of a command
    /// </summary>
    public interface ICommandContext
    {
        /// <summary>
        /// Loads the aggregate root with the specified type and ID, optionally creating it if it does not already exist
        /// </summary>
        [Obsolete]
        TAggregateRoot Load<TAggregateRoot>(string aggregateRootId, bool createIfNotExists = false) where TAggregateRoot : AggregateRoot, new();

        /// <summary>
        /// Creates an aggregate root of the given type with the given ID. Throws if an instance already exists with the given ID
        /// </summary>
        TAggregateRoot Create<TAggregateRoot>(string aggregateRootId) where TAggregateRoot : AggregateRoot, new();

        /// <summary>
        /// Attempts to load the given aggregate root, returning null if it does not exist
        /// </summary>
        TAggregateRoot TryLoad<TAggregateRoot>(string aggregateRootId) where TAggregateRoot : AggregateRoot, new();
        
        /// <summary>
        /// Attempts to load the given aggregate root, throwing an exception if it does not exist
        /// </summary>
        TAggregateRoot Load<TAggregateRoot>(string aggregateRootId) where TAggregateRoot : AggregateRoot, new();
    }
}
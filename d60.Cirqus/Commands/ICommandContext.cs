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
        TAggregateRoot Load<TAggregateRoot>(string aggregateRootId, bool createIfNotExists = false) where TAggregateRoot : AggregateRoot, new();
    }
}
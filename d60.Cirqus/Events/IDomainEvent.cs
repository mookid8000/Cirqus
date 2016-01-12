using d60.Cirqus.Numbers;

namespace d60.Cirqus.Events
{
    /// <summary>
    /// Interface that all domain events implements
    /// </summary>
    public interface IDomainEvent
    {
        /// <summary>
        /// Gets the domain event's metadata
        /// </summary>
        Metadata Meta { get; }
    }
}
using d60.Cirqus.Aggregates;
using d60.Cirqus.Events;

namespace d60.Cirqus.Views.ViewManagers
{
    /// <summary>
    /// Defines a context of stuff that a view can take advantage of
    /// </summary>
    public interface IViewContext
    {
        /// <summary>
        /// Loads the specified aggregate root snapshot as it looked after emitting the event currently being handled
        /// </summary>
        TAggregateRoot Load<TAggregateRoot>(string aggregateRootId) where TAggregateRoot : AggregateRoot, new();

        /// <summary>
        /// Loads the specified aggregate root snapshot as it looked at the time after the event with the specified global sequence number
        /// </summary>
        TAggregateRoot Load<TAggregateRoot>(string aggregateRootId, long globalSequenceNumber) where TAggregateRoot : AggregateRoot, new();

        /// <summary>
        /// Gets/sets the domain event currently being handled - should be set only from within a view manager
        /// </summary>
        DomainEvent CurrentEvent { get; set; }
    }
}
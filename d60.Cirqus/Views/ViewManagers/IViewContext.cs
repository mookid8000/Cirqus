using d60.Cirqus.Events;

namespace d60.Cirqus.Views.ViewManagers
{
    /// <summary>
    /// Defines a context of stuff that a view can take advantage of
    /// </summary>
    public interface IViewContext
    {
        /// <summary>
        /// Loads the specified aggregate root snapshot as it looked after emitting the event currently being handled, throwing an exception if an instance with that ID does not exist
        /// </summary>
        TAggregateRoot Load<TAggregateRoot>(string aggregateRootId) where TAggregateRoot : class;

        /// <summary>
        /// Loads the specified aggregate root snapshot as it looked at the time after the event with the specified global sequence number, throwing an exception if an instance with that ID did not exist at that time
        /// </summary>
        TAggregateRoot Load<TAggregateRoot>(string aggregateRootId, long globalSequenceNumber) where TAggregateRoot : class;

        /// <summary>
        /// Loads the specified aggregate root snapshot as it looked after emitting the event currently being handled, returning null if an instance with that ID does not exist
        /// </summary>
        TAggregateRoot TryLoad<TAggregateRoot>(string aggregateRootId) where TAggregateRoot : class;

        /// <summary>
        /// Loads the specified aggregate root snapshot as it looked at the time after the event with the specified global sequence number, returning null if an instance with that ID did not exist at that time
        /// </summary>
        TAggregateRoot TryLoad<TAggregateRoot>(string aggregateRootId, long globalSequenceNumber) where TAggregateRoot : class;

        /// <summary>
        /// Gets/sets the domain event currently being handled - should be set only from within a view manager
        /// </summary>
        DomainEvent CurrentEvent { get; set; }

        ///// <summary>
        ///// Marks this view instance for deletion
        ///// </summary>
        //void DeleteThisViewInstance();
    }
}
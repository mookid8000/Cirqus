using d60.Cirqus.Aggregates;

namespace d60.Cirqus.Snapshotting.New
{
    /// <summary>
    /// Abstraction of a place to (maybe persistently) save and retrieve aggregate root snapshots.
    /// </summary>
    public interface ISnapshotStore
    {
        /// <summary>
        /// Saves the given snapshot with information that it can be used from the given global sequence number <paramref name="validFromGlobalSequenceNumber"/> and on. 
        /// </summary>
        void SaveSnapshot<TAggregateRoot>(string aggregateRootId, AggregateRoot aggregateRootInstance, long validFromGlobalSequenceNumber);

        /// <summary>
        /// Loads the first snapshot that can be used as a view of the world at the time specified by <paramref name="maxGlobalSequenceNumber"/>, though it might need to be brought up-to-date
        /// </summary>
        Snapshot LoadSnapshot<TAggregateRoot>(string aggregateRootId, long maxGlobalSequenceNumber);
    }
}
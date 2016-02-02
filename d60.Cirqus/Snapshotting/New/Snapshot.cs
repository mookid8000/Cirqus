using d60.Cirqus.Aggregates;

namespace d60.Cirqus.Snapshotting.New
{
    /// <summary>
    /// Represents an aggregate root at a certain instant in time
    /// </summary>
    public class Snapshot
    {
        /// <summary>
        /// Creates the snapshot instance
        /// </summary>
        public Snapshot(long validFromGlobalSequenceNumber, AggregateRoot instance)
        {
            ValidFromGlobalSequenceNumber = validFromGlobalSequenceNumber;
            Instance = instance;
        }

        /// <summary>
        /// Indicates a global sequence number from which this aggregate root can be used.
        /// 
        ///                                                                                  T
        /// |=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=| global events
        ///                         |=---------------------=---------------=-=-------------=----| one particular aggregate root's events
        ///                          0                     1               2 3   /\        4
        ///                                                                      ||
        ///                                                                      || 
        /// Consider e.g. that <see cref="ValidFromGlobalSequenceNumber"/> is at the position specified
        /// by the fat vertical arrow above. It means that the root has had events 0 through 3 applied.
        /// Now we want the aggregate in a version that matches the position illustrated by the T above,
        /// so we would need to replay from <see cref="ValidFromGlobalSequenceNumber"/> until we got to
        /// T in order to know that we have an up-to-date version of the root (in this case applying event 4
        /// too) 
        /// </summary>
        public long ValidFromGlobalSequenceNumber { get; private set; }
        
        /// <summary>
        /// Gets the instance
        /// </summary>
        public AggregateRoot Instance { get; private set; }
    }
}
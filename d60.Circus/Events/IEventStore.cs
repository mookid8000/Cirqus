using System;
using System.Collections.Generic;

namespace d60.Circus.Events
{
    /// <summary>
    /// Event store that is capable of atomically and idempotently saving a batch of events
    /// </summary>
    public interface IEventStore
    {
        /// <summary>
        /// Saves the specified batch of events as an idempotent and atomic operation
        /// </summary>
        void Save(Guid batchId, IEnumerable<DomainEvent> batch);

        /// <summary>
        /// Loads events for the specified aggregate root
        /// </summary>
        IEnumerable<DomainEvent> Load(Guid aggregateRootId, long firstSeq = 0, long limit = int.MaxValue);

        /// <summary>
        /// Looks up the next available sequence number for that particular aggregate root ID
        /// </summary>
        long GetNextSeqNo(Guid aggregateRootId);

        /// <summary>
        /// Streams all events with a global sequence number that is greater than or equal to the one given
        /// </summary>
        IEnumerable<DomainEvent> Stream(long globalSequenceNumber = 0);
    }
}
using System;
using System.Collections.Generic;

namespace d60.Cirqus.Events
{
    /// <summary>
    /// Event store that is capable of atomically and idempotently saving a batch of events
    /// </summary>
    public interface IEventStore
    {
        /// <summary>
        /// Saves the specified batch of events as an idempotent and atomic operation
        /// </summary>
        void Save(Guid batchId, IEnumerable<Event> batch);

        /// <summary>
        /// Loads events for the specified aggregate root
        /// </summary>
        IEnumerable<Event> Load(string aggregateRootId, long firstSeq = 0);

        /// <summary>
        /// Streams all events with a global sequence number that is greater than or equal to the one given
        /// </summary>
        IEnumerable<Event> Stream(long globalSequenceNumber = 0);

        /// <summary>
        /// Gets the next available global sequence number. This will be the number used on the first event in the next saved event batch.
        /// </summary>
        long GetNextGlobalSequenceNumber();
    }
}
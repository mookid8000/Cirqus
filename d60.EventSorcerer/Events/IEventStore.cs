using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace d60.EventSorcerer.Events
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
        IEnumerable<DomainEvent> Load(Guid aggregateRootId, int firstSeq = 0, int limit = int.MaxValue);
    }

    /// <summary>
    /// Exception that must be raised when an attempt to commit a batch of events has failed because one or more of the involved event sequence numbers have been taken
    /// </summary>
    public class ConcurrencyException : ApplicationException
    {
        public Guid BatchId { get; private set; }

        public ConcurrencyException(Guid batchId, IEnumerable<DomainEvent> involvedDomainEvents, Exception innerException)
            : base(FormatErrorMessage(batchId, involvedDomainEvents), innerException)
        {
            BatchId = batchId;
        }

        public ConcurrencyException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        static string FormatErrorMessage(Guid batchId, IEnumerable<DomainEvent> involvedDomainEvents)
        {
            var sequenceNumbers = involvedDomainEvents.Select(e => e.Meta[DomainEvent.MetadataKeys.SequenceNumber]);
            var sequenceNumbersText = string.Join(", ", sequenceNumbers);

            return string.Format("Could not save batch {0} containing {1} to the event store because someone else beat us to it", batchId, sequenceNumbersText);
        }
    }
}
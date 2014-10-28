using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using d60.Cirqus.Events;

namespace d60.Cirqus.Exceptions
{
    /// <summary>
    /// Exception that must be raised when an attempt to commit a batch of events has failed because one or more of the involved event sequence numbers have been taken
    /// </summary>
    [Serializable]
    public class ConcurrencyException : ApplicationException
    {
        public ConcurrencyException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        public ConcurrencyException(Guid batchId, IEnumerable<DomainEvent> involvedDomainEvents, Exception innerException)
            : base(FormatErrorMessage(batchId, involvedDomainEvents), innerException)
        {
        }

        public ConcurrencyException(Guid batchId, IEnumerable<Event> involvedDomainEvents, Exception innerException)
            : base(FormatErrorMessage(batchId, involvedDomainEvents), innerException)
        {
            
        }

        static string FormatErrorMessage(Guid batchId, IEnumerable<Event> involvedDomainEvents)
        {
            var sequenceNumbersText = string.Join(Environment.NewLine, involvedDomainEvents
                .Select(e => "    " + e));

            return string.Format(@"Could not save batch {0} containing

{1}

to the event store because someone else beat us to it", batchId, sequenceNumbersText);
        }

        static string FormatErrorMessage(Guid batchId, IEnumerable<DomainEvent> involvedDomainEvents)
        {
            var sequenceNumbersText = string.Join(Environment.NewLine, involvedDomainEvents
                .Select(e => "    " + e));

            return string.Format(@"Could not save batch {0} containing

{1}

to the event store because someone else beat us to it", batchId, sequenceNumbersText);
        }
    }
}
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using d60.Cirqus.Events;
using d60.Cirqus.Extensions;

namespace d60.Cirqus.Views.ViewManagers
{
    /// <summary>
    /// Special "domain event" that wraps a batch of real domain events. Can be used in a view subsystem to give access to an entire batch
    /// of domain events
    /// </summary>
    public class DomainEventBatch : DomainEvent, IEnumerable<DomainEvent>
    {
        readonly List<DomainEvent> _domainEvents = new List<DomainEvent>();

        /// <summary>
        /// Constructs the <see cref="DomainEventBatch"/> with the given domain events
        /// </summary>
        public DomainEventBatch(IEnumerable<DomainEvent> batch)
        {
            _domainEvents.AddRange(batch);

            var maxGlobalSequenceNumber = _domainEvents.Max(d => d.GetGlobalSequenceNumber());

            Meta[MetadataKeys.GlobalSequenceNumber] = maxGlobalSequenceNumber.ToString();
        }

        public IEnumerator<DomainEvent> GetEnumerator()
        {
            return _domainEvents.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
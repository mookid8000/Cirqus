using System.Collections;
using System.Collections.Generic;
using System.Linq;
using d60.Cirqus.Events;
using d60.Cirqus.Extensions;

namespace d60.Cirqus.Testing
{
    public class CommandProcessingResultWithEvents : CommandProcessingResult, IEnumerable<DomainEvent>
    {
        readonly List<DomainEvent> _events;

        public CommandProcessingResultWithEvents(IEnumerable<DomainEvent> events)
            : base(events.Any() ? events.Max(e => e.GetGlobalSequenceNumber()) : default(long?))
        {
            _events = events.ToList();
        }

        public IEnumerator<DomainEvent> GetEnumerator()
        {
            return _events.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
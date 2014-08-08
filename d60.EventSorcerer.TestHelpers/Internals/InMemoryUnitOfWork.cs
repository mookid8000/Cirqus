using System.Collections;
using System.Collections.Generic;
using d60.EventSorcerer.Commands;
using d60.EventSorcerer.Events;

namespace d60.EventSorcerer.TestHelpers.Internals
{
    class InMemoryUnitOfWork : RealUnitOfWork, IEnumerable<DomainEvent>
    {
        public void Clear()
        {
            Events.Clear();
            CachedAggregateRoots.Clear();
        }

        public IEnumerator<DomainEvent> GetEnumerator()
        {
            return Events.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
using System.Collections;
using System.Collections.Generic;
using d60.Circus.Commands;
using d60.Circus.Events;

namespace d60.Circus.TestHelpers.Internals
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
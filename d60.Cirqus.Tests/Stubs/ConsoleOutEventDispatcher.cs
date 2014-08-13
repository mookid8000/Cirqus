using System;
using System.Collections.Generic;
using System.Linq;
using d60.Cirqus.Events;

namespace d60.Cirqus.Tests.Stubs
{
    public class ConsoleOutEventDispatcher : IEventDispatcher
    {
        public void Initialize(IEventStore eventStore, bool purgeExistingViews = false)
        {
            Console.WriteLine("Ignoring {0} events", eventStore.Stream().Count());
        }

        public void Dispatch(IEventStore eventStore, IEnumerable<DomainEvent> events)
        {
            foreach (var e in events)
            {
                Console.WriteLine(e);
            }
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using d60.Circus.Events;

namespace d60.Circus.Tests.Stubs
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
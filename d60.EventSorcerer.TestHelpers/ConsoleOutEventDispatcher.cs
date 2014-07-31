using System;
using System.Collections.Generic;
using d60.EventSorcerer.Events;

namespace d60.EventSorcerer.TestHelpers
{
    public class ConsoleOutEventDispatcher : IEventDispatcher
    {
        public void Dispatch(IEventStore eventStore, IEnumerable<DomainEvent> events)
        {
            foreach (var e in events)
            {
                Console.WriteLine(e);
            }
        }
    }
}
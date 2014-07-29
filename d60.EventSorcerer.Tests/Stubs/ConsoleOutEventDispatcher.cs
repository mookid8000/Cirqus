using System;
using System.Collections.Generic;
using d60.EventSorcerer.Events;
using d60.EventSorcerer.Views;

namespace d60.EventSorcerer.Tests.Stubs
{
    public class ConsoleOutEventDispatcher : IEventDispatcher
    {
        public void Dispatch(IEnumerable<DomainEvent> events)
        {
            foreach (var e in events)
            {
                Console.WriteLine(e);
            }
        }
    }
}
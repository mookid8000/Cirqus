using System;
using d60.EventSorcerer.Events;

namespace d60.EventSorcerer.TestHelpers
{
    public class ConsoleOutEventCollector : IEventCollector
    {
        public void Add(DomainEvent e)
        {
            Console.WriteLine("Emitted: {0}", e);
        }
    }
}
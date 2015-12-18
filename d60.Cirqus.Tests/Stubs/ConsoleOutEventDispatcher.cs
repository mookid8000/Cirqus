using System;
using System.Collections.Generic;
using System.Linq;
using d60.Cirqus.Events;
using d60.Cirqus.Views;

namespace d60.Cirqus.Tests.Stubs
{
    public class ConsoleOutEventDispatcher : IEventDispatcher
    {
        readonly IEventStore _eventStore;

        public ConsoleOutEventDispatcher(IEventStore eventStore)
        {
            _eventStore = eventStore;
        }

        public void Initialize(bool purgeExistingViews = false)
        {
            Console.WriteLine("Ignoring {0} events", _eventStore.Stream().Count());
        }

        public void Dispatch(IEnumerable<DomainEvent> events)
        {
            foreach (var e in events)
            {
                Console.WriteLine(e);
            }
        }
    }
}
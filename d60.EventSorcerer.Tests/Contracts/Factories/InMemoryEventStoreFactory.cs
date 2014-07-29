using d60.EventSorcerer.Events;
using d60.EventSorcerer.Numbers;
using d60.EventSorcerer.Tests.Stubs;

namespace d60.EventSorcerer.Tests.Contracts.Factories
{
    public class InMemoryEventStoreFactory : IEventStoreFactory
    {
        readonly InMemoryEventStore _eventStore;

        public InMemoryEventStoreFactory()
        {
            _eventStore = new InMemoryEventStore();
        }

        public IEventStore GetEventStore()
        {
            return _eventStore;
        }

        public ISequenceNumberGenerator GetSequenceNumberGenerator()
        {
            return _eventStore;
        }
    }
}
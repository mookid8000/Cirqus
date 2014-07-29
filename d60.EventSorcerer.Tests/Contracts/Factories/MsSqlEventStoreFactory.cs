using d60.EventSorcerer.Events;
using d60.EventSorcerer.MsSql;
using d60.EventSorcerer.Numbers;

namespace d60.EventSorcerer.Tests.Contracts.Factories
{
    public class MsSqlEventStoreFactory : IEventStoreFactory
    {
        readonly MsSqlEventStore _eventStore;

        public MsSqlEventStoreFactory()
        {
            _eventStore = new MsSqlEventStore("testdb", "events");
            
            _eventStore.DropEvents();
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
using d60.Cirqus.Events;
using d60.Cirqus.NTFS.Events;

namespace d60.Cirqus.Tests.Contracts.EventStore.Factories
{
    public class NtfsEventStoreFactory : IEventStoreFactory
    {
        readonly NtfsEventStore _eventStore;

        public NtfsEventStoreFactory()
        {
            _eventStore = new NtfsEventStore("testdata", dropEvents: true);
        }

        public IEventStore GetEventStore()
        {
            return _eventStore;
        }
    }
}
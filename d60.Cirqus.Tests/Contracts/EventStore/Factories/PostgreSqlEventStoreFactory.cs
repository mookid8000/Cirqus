using d60.Cirqus.Events;
using d60.Cirqus.PostgreSql;
using d60.Cirqus.Tests.MsSql;

namespace d60.Cirqus.Tests.Contracts.EventStore.Factories
{
    public class PostgreSqlEventStoreFactory : IEventStoreFactory
    {
        readonly PostgreSqlEventStore _eventStore;

        public PostgreSqlEventStoreFactory()
        {
            var connectionString = TestSqlHelper.PostgreSqlConnectionString;

            _eventStore = new PostgreSqlEventStore(connectionString, "Events");
        }

        public IEventStore GetEventStore()
        {
            return _eventStore;
        }
    }
}
using d60.Cirqus.Events;
using d60.Cirqus.PostgreSql;
using d60.Cirqus.PostgreSql.Events;
using d60.Cirqus.Tests.PostgreSql;

namespace d60.Cirqus.Tests.Contracts.EventStore.Factories
{
    public class PostgreSqlEventStoreFactory : IEventStoreFactory
    {
        readonly PostgreSqlEventStore _eventStore;

        public PostgreSqlEventStoreFactory()
        {
            PostgreSqlTestHelper.DropTable("Events");

            var connectionString = PostgreSqlTestHelper.PostgreSqlConnectionString;

            _eventStore = new PostgreSqlEventStore(connectionString, "Events");

            _eventStore.DropEvents();
        }

        public IEventStore GetEventStore()
        {
            return _eventStore;
        }
    }
}
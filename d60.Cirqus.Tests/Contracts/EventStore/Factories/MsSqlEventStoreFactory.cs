using d60.Cirqus.Events;
using d60.Cirqus.MsSql.Events;
using d60.Cirqus.Tests.MsSql;

namespace d60.Cirqus.Tests.Contracts.EventStore.Factories
{
    public class MsSqlEventStoreFactory : IEventStoreFactory
    {
        readonly MsSqlEventStore _eventStore;

        public MsSqlEventStoreFactory()
        {
            MsSqlTestHelper.EnsureTestDatabaseExists();

            var connectionString = MsSqlTestHelper.ConnectionString;
            
            MsSqlTestHelper.DropTable("events");

            _eventStore = new MsSqlEventStore(connectionString, "events");
            
            _eventStore.DropEvents();
        }

        public IEventStore GetEventStore()
        {
            return _eventStore;
        }
    }
}
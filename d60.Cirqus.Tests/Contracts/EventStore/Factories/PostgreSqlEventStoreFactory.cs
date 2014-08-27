using d60.Cirqus.Events;
using d60.Cirqus.PostgreSql;
using d60.Cirqus.Tests.MsSql;
using Npgsql;

namespace d60.Cirqus.Tests.Contracts.EventStore.Factories
{
    public class PostgreSqlEventStoreFactory : IEventStoreFactory
    {
        readonly PostgreSqlEventStore _eventStore;
        readonly string _connectionString;

        public PostgreSqlEventStoreFactory()
        {
            _connectionString = TestSqlHelper.PostgreSqlConnectionString;

            DropTable();

            _eventStore = new PostgreSqlEventStore(_connectionString, "Events");

            _eventStore.DropEvents();
        }

        public IEventStore GetEventStore()
        {
            return _eventStore;
        }

        void DropTable()
        {
            using (var connection = new NpgsqlConnection(_connectionString))
            {
                using (var cmd = connection.CreateCommand())
                {
                    connection.Open();

                    cmd.CommandText = @"DROP TABLE IF EXISTS ""Events"" CASCADE";
                    
                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}
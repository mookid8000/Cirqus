using System;
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

            DropTable("Events");

            _eventStore = new PostgreSqlEventStore(_connectionString, "Events");

            _eventStore.DropEvents();
        }

        public IEventStore GetEventStore()
        {
            return _eventStore;
        }

        void DropTable(string tableName)
        {
            Console.WriteLine("Dropping Postgres table '{0}'", tableName);

            using (var connection = new NpgsqlConnection(_connectionString))
            {
                using (var cmd = connection.CreateCommand())
                {
                    connection.Open();

                    cmd.CommandText = string.Format(@"DROP TABLE IF EXISTS ""{0}"" CASCADE", tableName);
                    
                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}
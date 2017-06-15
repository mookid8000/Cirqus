using System;
using d60.Cirqus.Config.Configurers;
using d60.Cirqus.Events;
using d60.Cirqus.PostgreSql.Events;
using Npgsql;

namespace d60.Cirqus.PostgreSql.Config
{
    public static class PostgreSqlConfigurationExtensions
    {
        /// <summary>
        /// Configures Cirqus to use Postgres as the event store
        /// </summary>
        public static void UsePostgreSql(this EventStoreConfigurationBuilder builder, string connectionStringOrConnectionStringName, string tableName, bool automaticallyCreateSchema = true, Action<NpgsqlConnection> additionalConnectionSetup = null)
        {
            if (builder == null) throw new ArgumentNullException("builder");
            if (connectionStringOrConnectionStringName == null) throw new ArgumentNullException("connectionStringOrConnectionStringName");
            if (tableName == null) throw new ArgumentNullException("tableName");

            builder.Register<IEventStore>(context => new PostgreSqlEventStore(connectionStringOrConnectionStringName, tableName, automaticallyCreateSchema: automaticallyCreateSchema, additionalConnectionSetup: additionalConnectionSetup));
        }
    }
}
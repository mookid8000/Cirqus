using System;
using d60.Cirqus.Config.Configurers;
using d60.Cirqus.Events;

namespace d60.Cirqus.PostgreSql.Config
{
    public static class PostgreSqlConfigurationExtensions
    {
        /// <summary>
        /// Configures Cirqus to use Postgres as the event store
        /// </summary>
        public static void UsePostgreSql(this EventStoreConfigurationBuilder builder, string connectionStringOrConnectionStringName, string tableName, bool automaticallyCreateSchema = true)
        {
            if (builder == null) throw new ArgumentNullException("builder");
            if (connectionStringOrConnectionStringName == null) throw new ArgumentNullException("connectionStringOrConnectionStringName");
            if (tableName == null) throw new ArgumentNullException("tableName");

            builder.Registrar.Register<IEventStore>(context => new PostgreSqlEventStore(connectionStringOrConnectionStringName, tableName, automaticallyCreateSchema: automaticallyCreateSchema));
        }
    }
}
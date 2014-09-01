using System;
using d60.Cirqus.Config.Configurers;
using d60.Cirqus.Events;

namespace d60.Cirqus.PostgreSql.Config
{
    public static class PostgreSqlConfigurationExtensions
    {
        public static void UsePostgreSql(this EventStoreConfigurationBuilder builder, string connectionStringOrConnectionStringName, string tableName, bool automaticallyCreateSchema = true)
        {
            if (builder == null) throw new ArgumentNullException("builder");
            if (connectionStringOrConnectionStringName == null) throw new ArgumentNullException("connectionStringOrConnectionStringName");
            if (tableName == null) throw new ArgumentNullException("tableName");

            builder.Registrar.Register<IEventStore>(() => new PostgreSqlEventStore(connectionStringOrConnectionStringName, tableName, automaticallyCreateSchema: automaticallyCreateSchema));
        }
    }
}
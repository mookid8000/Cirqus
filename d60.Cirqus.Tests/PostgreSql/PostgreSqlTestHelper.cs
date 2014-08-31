using System;
using d60.Cirqus.MsSql;
using Npgsql;

namespace d60.Cirqus.Tests.PostgreSql
{
    class PostgreSqlTestHelper : SqlTestHelperBase
    {
        public static string PostgreSqlConnectionString
        {
            get
            {
                var connectionString = SqlHelper.GetConnectionString("postgresqltestdb");

                var configuredDatabaseName = GetDatabaseName(connectionString);

                var databaseNameToUse = PossiblyAppendTeamcityAgentNumber(configuredDatabaseName);

                Console.WriteLine("Using test POSTGRESQL database '{0}'", databaseNameToUse);

                return connectionString.Replace(configuredDatabaseName, databaseNameToUse);
            }
        }

        public static void DropTable(string tableName)
        {
            Console.WriteLine("Dropping Postgres table '{0}'", tableName);

            using (var connection = new NpgsqlConnection(PostgreSqlConnectionString))
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
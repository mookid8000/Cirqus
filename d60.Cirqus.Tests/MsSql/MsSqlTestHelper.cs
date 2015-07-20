using System;
using System.Data.SqlClient;
using System.Linq;
using d60.Cirqus.MsSql;

namespace d60.Cirqus.Tests.MsSql
{
    class MsSqlTestHelper : SqlTestHelperBase
    {
        public static string ConnectionString
        {
            get
            {
                var connectionString = SqlHelper.GetConnectionString("sqltestdb");

                var configuredDatabaseName = GetDatabaseName(connectionString);

                var databaseNameToUse = PossiblyAppendTeamcityAgentNumber(configuredDatabaseName);

                Console.WriteLine("Using test SQL database '{0}'", databaseNameToUse);

                return connectionString.Replace(configuredDatabaseName, databaseNameToUse);
            }
        }

        public static void EnsureTestDatabaseExists()
        {
            var connectionString = ConnectionString;
            var databaseName = GetDatabaseName(connectionString);
            var masterConnectionString = connectionString.Replace(databaseName, "master");

            try
            {
                using (var conn = new SqlConnection(masterConnectionString))
                {
                    conn.Open();

                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = string.Format(@"
BEGIN
    CREATE DATABASE [{0}]
END

", databaseName);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (SqlException exception)
            {
                if (exception.Errors.Cast<SqlError>().Any(e => e.Number == 1801))
                {
                    Console.WriteLine("Test database '{0}' already existed", databaseName);
                    return;
                }
                throw;
            }
        }

        public static void DropTable(string tableName)
        {
            try
            {
                using (var conn = new SqlConnection(ConnectionString))
                {
                    conn.Open();

                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = string.Format(@"
BEGIN
    DROP TABLE [{0}]
END

", tableName);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (SqlException exception)
            {
                if (exception.Number == 3701)
                {
                    Console.WriteLine("Table '{0}' was already gone", tableName);
                    return;
                }
                throw;
            }
        }
    }
}
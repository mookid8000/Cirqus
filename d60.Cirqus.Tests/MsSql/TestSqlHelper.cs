using System;
using System.Data.SqlClient;
using System.Linq;
using d60.Cirqus.MsSql;

namespace d60.Cirqus.Tests.MsSql
{
    public class TestSqlHelper
    {
        public static string PostgreSqlConnectionString
        {
            get { return "Server=localhost;Database=cirqus;User=postgres;Password=postgres;"; }
        }

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

        static string PossiblyAppendTeamcityAgentNumber(string databaseName)
        {
            var teamCityAgentNumber = Environment.GetEnvironmentVariable("tcagent");
            int number;

            if (string.IsNullOrWhiteSpace(teamCityAgentNumber) || !int.TryParse(teamCityAgentNumber, out number))
                return databaseName;

            return string.Format("{0}_agent{1}", databaseName, number);
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
                if (exception.Errors.Cast<SqlError>().Any(e => e.Number == 3701))
                {
                    Console.WriteLine("Table '{0}' was already gone", tableName);
                    return;
                }
                throw;
            }
        }

        public static string GetDatabaseName(string connectionString)
        {
            var relevantSetting = connectionString
                .Split(';')
                .Select(kvp =>
                {
                    var tokens = kvp.Split('=');

                    return new
                    {
                        Key = tokens[0],
                        Value = tokens.Length > 0 ? tokens[1] : null
                    };
                })
                .FirstOrDefault(a => string.Equals(a.Key, "database", StringComparison.InvariantCultureIgnoreCase));

            return relevantSetting != null ? relevantSetting.Value : null;
        }
    }
}
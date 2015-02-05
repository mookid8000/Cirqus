using System;
using System.Collections.Generic;
using System.Linq;
using d60.Cirqus.MsSql;
using Npgsql;

namespace d60.Cirqus.Tests.PostgreSql
{
    class PostgreSqlTestHelper : SqlTestHelperBase
    {
        static PostgreSqlTestHelper()
        {
            var namesOfExistingDatabases = GetDatabaseNames();
            var nameOfTestDatabase = GetDatabaseName();

            if (!namesOfExistingDatabases.Contains(nameOfTestDatabase))
            {
                CreateDatabase(nameOfTestDatabase);
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

        static void CreateDatabase(string databaseName)
        {
            Console.WriteLine("Creating Postgres database '{0}'", databaseName);

            var connectionString = GetMasterConnectionString();

            using (var connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = string.Format(@" create database ""{0}""", databaseName);

                    command.ExecuteNonQuery();
                }
            }
        }

        static IEnumerable<string> GetDatabaseNames()
        {
            var connectionString = GetMasterConnectionString();

            using (var connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = @"select * from pg_catalog.pg_database";

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            yield return reader["datname"].ToString();
                        }
                    }
                }
            }
        }

        static string GetMasterConnectionString()
        {
            return GetConnectionStringForAnotherDatabase(PostgreSqlConnectionString, GetDatabaseName(), "postgres");
        }

        public static string PostgreSqlConnectionString
        {
            get
            {
                var connectionString = SqlHelper.GetConnectionString("postgresqltestdb");

                var configuredDatabaseName = GetDatabaseName();

                var databaseNameToUse = PossiblyAppendTeamcityAgentNumber(configuredDatabaseName);

                Console.WriteLine("Using test POSTGRESQL database '{0}'", databaseNameToUse);

                return GetConnectionStringForAnotherDatabase(connectionString, configuredDatabaseName, databaseNameToUse);
            }
        }

        static string GetConnectionStringForAnotherDatabase(string connectionString, string configuredDatabaseName, string databaseNameToUse)
        {
            return connectionString.Replace(configuredDatabaseName, databaseNameToUse);
        }

        static string GetDatabaseName()
        {
            var connectionString = SqlHelper.GetConnectionString("postgresqltestdb");

            return GetDatabaseName(connectionString);
        }
    }
}
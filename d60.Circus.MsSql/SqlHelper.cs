using System;
using System.Configuration;
using System.Linq;

namespace d60.Circus.MsSql
{
    public class SqlHelper
    {
        /// <summary>
        /// Looks for a connection string in AppSettings with the specified name and returns that if possible - otherwise,
        /// it is assumed that the string is a connection string in itself
        /// </summary>
        public static string GetConnectionString(string connectionStringOrConnectionStringName)
        {
            var connectionStringSettings = ConfigurationManager.ConnectionStrings[connectionStringOrConnectionStringName];

            var connectionString = connectionStringSettings != null
                ? connectionStringSettings.ConnectionString
                : connectionStringOrConnectionStringName;

            var databaseName = GetDatabaseName(connectionString);
            var databaseNameToUse = PossiblyAppendTeamcityAgentNumber(databaseName);

            Console.WriteLine("Using SQL database for testing: '{0}'", databaseNameToUse);

            return connectionString.Replace(databaseName, databaseNameToUse);
        }

        public static string GetDatabaseName(string connectionString)
        {
            var originalDatabaseName = InnerGetDatabaseName(connectionString);
            var databaseNameToUse = PossiblyAppendTeamcityAgentNumber(originalDatabaseName);

            return databaseNameToUse;
        }

        static string PossiblyAppendTeamcityAgentNumber(string databaseName)
        {
            var teamCityAgentNumber = Environment.GetEnvironmentVariable("tcagent");
            int number;

            if (string.IsNullOrWhiteSpace(teamCityAgentNumber) || !int.TryParse(teamCityAgentNumber, out number))
                return databaseName;

            return string.Format("{0}_agent{1}", databaseName, number);
        }

        static string InnerGetDatabaseName(string connectionString)
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
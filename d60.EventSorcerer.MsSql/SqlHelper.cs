using System;
using System.Configuration;
using System.Linq;

namespace d60.EventSorcerer.MsSql
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

            return connectionString;
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
using System.Configuration;

namespace d60.Cirqus.NHibernate
{
    class SqlHelper
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
    }
}
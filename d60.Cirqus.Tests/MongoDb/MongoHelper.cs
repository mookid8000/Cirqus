using System;
using System.Configuration;
using MongoDB.Driver;

namespace d60.Cirqus.Tests.MongoDb
{
    public class MongoHelper
    {
        public static MongoDatabase InitializeTestDatabase(bool dropExistingDatabase = true)
        {
            var connectionStringSettings = ConfigurationManager.ConnectionStrings["mongotestdb"];
            if (connectionStringSettings == null)
            {
                throw new ConfigurationErrorsException("Could not find MongoDB test database connection string with the name 'mongotestdb' in app.config");
            }
            
            var url = new MongoUrl(connectionStringSettings.ConnectionString);
            var databaseName = GetDatabaseName(url);
            var database = new MongoClient(url).GetServer().GetDatabase(databaseName);

            Console.WriteLine("Using Mongo database '{0}'", databaseName);
            if (dropExistingDatabase)
            {
                Console.WriteLine("Dropping Mongo database '{0}'", databaseName);
                database.Drop();
            }

            return database;
        }

        public static string GetDatabaseName(MongoUrl url)
        {
            var databaseName = url.DatabaseName;

            var teamCityAgentNumber = Environment.GetEnvironmentVariable("tcagent");
            int number;

            if (string.IsNullOrWhiteSpace(teamCityAgentNumber) || !int.TryParse(teamCityAgentNumber, out number))
            {
                return databaseName;
            }

            return string.Format("{0}_{1}", databaseName, number);
        }
    }
}
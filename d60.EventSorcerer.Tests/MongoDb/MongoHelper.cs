using System.Configuration;
using MongoDB.Driver;

namespace d60.EventSorcerer.Tests.MongoDb
{
    public class MongoHelper
    {
        public static MongoDatabase InitializeTestDatabase()
        {
            var connectionStringSettings = ConfigurationManager.ConnectionStrings["mongotestdb"];
            if (connectionStringSettings == null)
            {
                throw new ConfigurationErrorsException("Could not find MongoDB test database connection string with the name 'mongotestdb' in app.config");
            }
            
            var url = new MongoUrl(connectionStringSettings.ConnectionString);
            var database = new MongoClient(url).GetServer().GetDatabase(url.DatabaseName);
            
            database.Drop();
            
            return database;
        } 
    }
}
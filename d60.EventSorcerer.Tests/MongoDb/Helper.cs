using MongoDB.Driver;

namespace d60.EventSorcerer.Tests.MongoDb
{
    public class Helper
    {
        public static MongoDatabase InitializeTestDatabase()
        {
            var database = new MongoClient().GetServer()
                .GetDatabase("es_test");
            
            database.Drop();
            
            return database;
        } 
    }
}
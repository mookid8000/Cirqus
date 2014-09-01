using System.Configuration;
using d60.Cirqus.Config;
using d60.Cirqus.Events;
using d60.Cirqus.MongoDb.Events;
using MongoDB.Driver;

namespace d60.Cirqus.MongoDb.Config
{
    public static class MongoDbConfigurationExtensions
    {
        public static void StoreInMongoDb(this EventStoreConfigurationBuilder builder, string mongoDbConnectionString, string eventCollectionName, bool automaticallyCreateIndexes = true)
        {
            builder.ServiceRegistrar
                .Register<IEventStore>(() =>
                {
                    var mongoUrl = new MongoUrl(mongoDbConnectionString);

                    if (string.IsNullOrEmpty(mongoUrl.DatabaseName))
                    {
                        throw new ConfigurationErrorsException(
                            "Please supply a database name as part of the MongoDB connection string!");
                    }

                    var database = new MongoClient(mongoUrl).GetServer()
                        .GetDatabase(mongoUrl.DatabaseName);

                    return new MongoDbEventStore(database, eventCollectionName,
                        automaticallyCreateIndexes: automaticallyCreateIndexes);
                });
        }
    }
}
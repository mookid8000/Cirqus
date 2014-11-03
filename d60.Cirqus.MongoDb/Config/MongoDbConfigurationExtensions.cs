using System;
using System.Configuration;
using d60.Cirqus.Config.Configurers;
using d60.Cirqus.Events;
using d60.Cirqus.Logging;
using d60.Cirqus.MongoDb.Events;
using d60.Cirqus.MongoDb.Logging;
using MongoDB.Driver;

namespace d60.Cirqus.MongoDb.Config
{
    public static class MongoDbConfigurationExtensions
    {
        public static MongoDbConfigBuilder UseMongoDb(this EventStoreConfigurationBuilder builder, string mongoDbConnectionString, string eventCollectionName, bool automaticallyCreateIndexes = true)
        {
            if (builder == null) throw new ArgumentNullException("builder");
            if (mongoDbConnectionString == null) throw new ArgumentNullException("mongoDbConnectionString");
            if (eventCollectionName == null) throw new ArgumentNullException("eventCollectionName");

            var mongoUrl = GetMongoUrl(mongoDbConnectionString);

            var database = new MongoClient(mongoUrl).GetServer()
                .GetDatabase(mongoUrl.DatabaseName);

            return UseMongoDbEventStore(builder, database, eventCollectionName, automaticallyCreateIndexes);
        }

        public static MongoDbConfigBuilder UseMongoDb(this EventStoreConfigurationBuilder builder, MongoDatabase database, string eventCollectionName, bool automaticallyCreateIndexes = true)
        {
            if (builder == null) throw new ArgumentNullException("builder");
            if (database == null) throw new ArgumentNullException("database");
            if (eventCollectionName == null) throw new ArgumentNullException("eventCollectionName");

            return UseMongoDbEventStore(builder, database, eventCollectionName, automaticallyCreateIndexes);
        }

        static MongoDbConfigBuilder UseMongoDbEventStore(EventStoreConfigurationBuilder builder, MongoDatabase database, string eventCollectionName, bool automaticallyCreateIndexes)
        {
            var configBuilder = new MongoDbConfigBuilder();

            builder.Registrar.Register<IEventStore>(context => new MongoDbEventStore(database, eventCollectionName, automaticallyCreateIndexes: automaticallyCreateIndexes));

            return configBuilder;
        }

        public static void UseMongoDb(this LoggingConfigurationBuilder builder, string mongoDbConnectionString, string logCollectionName)
        {
            if (builder == null) throw new ArgumentNullException("builder");
            if (mongoDbConnectionString == null) throw new ArgumentNullException("mongoDbConnectionString");
            if (logCollectionName == null) throw new ArgumentNullException("logCollectionName");

            var mongoUrl = GetMongoUrl(mongoDbConnectionString);

            var database = new MongoClient(mongoUrl).GetServer()
                .GetDatabase(mongoUrl.DatabaseName);

            UseMongoDbLoggerFactory(database, logCollectionName);
        }

        public static void UseMongoDb(this LoggingConfigurationBuilder builder, MongoDatabase database, string logCollectionName)
        {
            if (builder == null) throw new ArgumentNullException("builder");
            if (database == null) throw new ArgumentNullException("database");
            if (logCollectionName == null) throw new ArgumentNullException("logCollectionName");

            UseMongoDbLoggerFactory(database, logCollectionName);
        }

        static void UseMongoDbLoggerFactory(MongoDatabase database, string logCollectionName)
        {
            CirqusLoggerFactory.Current = new MongoDbLoggerFactory(database, logCollectionName);
        }

        static MongoUrl GetMongoUrl(string mongoDbConnectionString)
        {
            var mongoUrl = new MongoUrl(mongoDbConnectionString);

            if (string.IsNullOrEmpty(mongoUrl.DatabaseName))
            {
                throw new ConfigurationErrorsException(
                    "Please supply a database name as part of the MongoDB connection string!");
            }

            return mongoUrl;
        }
    }
}
using System;
using d60.Cirqus.Aggregates;
using d60.Cirqus.Config.Configurers;
using d60.Cirqus.Events;
using d60.Cirqus.Serialization;
using MongoDB.Driver;

namespace d60.Cirqus.MongoDb.Snapshotting
{
    public static class NewSnapshottingConfigurationExtensions
    {
        [Obsolete("This method is going to go away again")]
        public static void EnableExperimentalMongoDbSnapshotting(this OptionsConfigurationBuilder builder, MongoDatabase database, string collectionName)
        {
            builder.Decorate<IAggregateRootRepository>(c =>
            {
                var aggregateRootRepository = c.Get<IAggregateRootRepository>();
                var eventStore = c.Get<IEventStore>();
                var domainEventSerializer = c.Get<IDomainEventSerializer>();

                return new NewSnapshottingAggregateRootRepositoryDecorator(aggregateRootRepository, eventStore, domainEventSerializer, collectionName, database);
            });
        }
    }
}
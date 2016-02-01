using d60.Cirqus.Snapshotting.New;
using MongoDB.Driver;

namespace d60.Cirqus.MongoDb.Snapshotting
{
    public static class NewSnapshottingConfigurationExtensions
    {
        public static void UseMongoDb(this SnapshottingConfigurationBuilder builder, MongoDatabase database, string collectionName)
        {
            builder.Register(c => new MongoDbSnapshotStore(database, collectionName));
        }
    }
}
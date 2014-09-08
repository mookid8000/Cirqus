using d60.Cirqus.MongoDb.Views.New;
using d60.Cirqus.Tests.MongoDb;
using d60.Cirqus.Views.ViewManagers.New;
using MongoDB.Driver;

namespace d60.Cirqus.Tests.Contracts.Views.New.Factories
{
    public class MongoDbManagedViewFactory : ManagedViewFactoryBase
    {
        readonly MongoDatabase _database;

        public MongoDbManagedViewFactory()
        {
            _database = MongoHelper.InitializeTestDatabase();
        }

        public override IManagedView<TViewInstance> CreateManagedView<TViewInstance>()
        {
            var managedView = new NewMongoDbViewManager<TViewInstance>(_database);

            ManagedViews.Add(managedView);

            return managedView;
        }
    }
}
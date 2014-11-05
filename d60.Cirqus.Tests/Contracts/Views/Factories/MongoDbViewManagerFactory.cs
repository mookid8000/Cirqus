using d60.Cirqus.MongoDb.Views;
using d60.Cirqus.Tests.MongoDb;
using d60.Cirqus.Views.ViewManagers;
using MongoDB.Driver;

namespace d60.Cirqus.Tests.Contracts.Views.Factories
{
    public class MongoDbViewManagerFactory : AbstractViewManagerFactory
    {
        readonly MongoDatabase _database;

        public MongoDbViewManagerFactory()
        {
            _database = MongoHelper.InitializeTestDatabase();
        }

        protected override IViewManager<TViewInstance> CreateViewManager<TViewInstance>()
        {
            var viewManager = new MongoDbViewManager<TViewInstance>(_database);
            
            return viewManager;
        }
    }
}
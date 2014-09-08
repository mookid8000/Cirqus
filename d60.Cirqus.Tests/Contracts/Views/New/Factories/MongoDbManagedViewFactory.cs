using System;
using System.Collections.Generic;
using System.Linq;
using d60.Cirqus.MongoDb.Views.New;
using d60.Cirqus.Tests.MongoDb;
using d60.Cirqus.Views.ViewManagers;
using d60.Cirqus.Views.ViewManagers.New;
using MongoDB.Driver;

namespace d60.Cirqus.Tests.Contracts.Views.New.Factories
{
    public class MongoDbManagedViewFactory : IManagedViewFactory
    {
        readonly MongoDatabase _database;
        readonly List<IManagedView> _managedViews = new List<IManagedView>();

        public MongoDbManagedViewFactory()
        {
            _database = MongoHelper.InitializeTestDatabase();
        }

        public IManagedView<TViewInstance> CreateManagedView<TViewInstance>() where TViewInstance : class, IViewInstance, ISubscribeTo, new()
        {
            var managedView = new NewMongoDbViewManager<TViewInstance>(_database);

            _managedViews.Add(managedView);

            return managedView;
        }

        public TViewInstance Load<TViewInstance>(string viewId) where TViewInstance : class, IViewInstance, ISubscribeTo, new()
        {
            var managedView = _managedViews
                .OfType<IManagedView<TViewInstance>>()
                .FirstOrDefault();

            if (managedView == null)
            {
                throw new ArgumentException(string.Format("Could not find managed view for {0} - only have {1}",
                    typeof(TViewInstance), string.Join(", ", _managedViews)));
            }

            return managedView.Load(viewId);
        }
    }
}
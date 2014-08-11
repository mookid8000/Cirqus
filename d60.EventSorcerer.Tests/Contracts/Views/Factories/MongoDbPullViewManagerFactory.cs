using System;
using System.Collections.Generic;
using System.Linq;
using d60.EventSorcerer.MongoDb.Views;
using d60.EventSorcerer.Tests.MongoDb;
using d60.EventSorcerer.Views.Basic;
using MongoDB.Driver;

namespace d60.EventSorcerer.Tests.Contracts.Views.Factories
{
    class MongoDbPullViewManagerFactory : IPullViewManagerFactory, IPushViewManagerFactory
    {
        readonly List<IViewManager> _viewManagers = new List<IViewManager>();
        readonly MongoDatabase _database;

        public MongoDbPullViewManagerFactory()
        {
            _database = MongoHelper.InitializeTestDatabase();
        }

        public IPullViewManager GetPullViewManager<TView>() where TView : class, IViewInstance, ISubscribeTo, new()
        {
            var viewManager = GetMongoDbViewManager<TView>();

            return new PullOnlyWrapper(viewManager);
        }

        public IPushViewManager GetPushViewManager<TView>() where TView : class, IViewInstance, ISubscribeTo, new()
        {
            var viewManager = GetMongoDbViewManager<TView>();

            return new PushOnlyWrapper(viewManager);
        }

        MongoDbViewManager<TView> GetMongoDbViewManager<TView>() where TView : class, IViewInstance, ISubscribeTo, new()
        {
            var viewManager = new MongoDbViewManager<TView>(_database, typeof (TView).Name);

            MaxDomainEventsBetweenFlushSet += maxEvents => viewManager.MaxDomainEventsBetweenFlush = maxEvents;

            _viewManagers.Add(viewManager);
            return viewManager;
        }

        public TView Load<TView>(string viewId) where TView : class, IViewInstance, ISubscribeTo, new()
        {
            var viewManager = _viewManagers.OfType<MongoDbViewManager<TView>>().FirstOrDefault();

            if (viewManager == null)
            {
                throw new ApplicationException(string.Format("Could not find view manager for views of type {0}", typeof(TView)));
            }

            return viewManager.Load(viewId);
        }

        event Action<int> MaxDomainEventsBetweenFlushSet = delegate { }; 

        public void SetMaxDomainEventsBetweenFlush(int value)
        {
            MaxDomainEventsBetweenFlushSet(value);
        }
    }
}
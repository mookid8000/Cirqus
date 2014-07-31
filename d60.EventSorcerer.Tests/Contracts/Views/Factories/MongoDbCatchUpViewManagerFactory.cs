using System;
using System.Collections.Generic;
using System.Linq;
using d60.EventSorcerer.MongoDb.Views;
using d60.EventSorcerer.Tests.MongoDb;
using d60.EventSorcerer.Views.Basic;
using MongoDB.Driver;

namespace d60.EventSorcerer.Tests.Contracts.Views.Factories
{
    class MongoDbCatchUpViewManagerFactory : ICatchUpViewManagerFactory
    {
        readonly List<IViewManager> _viewManagers = new List<IViewManager>();
        readonly MongoDatabase _database;

        public MongoDbCatchUpViewManagerFactory()
        {
            _database = Helper.InitializeTestDatabase();
        }

        public IViewManager GetViewManagerFor<TView>() where TView : class, IView, ISubscribeTo, new()
        {
            var viewManager = new MongoDbCatchUpViewManager<TView>(_database, typeof(TView).Name);

            MaxDomainEventsBetweenFlushSet += maxEvents => viewManager.MaxDomainEventsBetweenFlush = maxEvents;

            _viewManagers.Add(viewManager);

            return viewManager;
        }

        public TView Load<TView>(string id) where TView : class, IView, ISubscribeTo, new()
        {
            var viewManager = _viewManagers.OfType<MongoDbCatchUpViewManager<TView>>().FirstOrDefault();

            if (viewManager == null)
            {
                throw new ApplicationException(string.Format("Could not find view manager for views of type {0}", typeof(TView)));
            }

            return viewManager.Load(id);
        }

        event Action<int> MaxDomainEventsBetweenFlushSet = delegate { }; 

        public void SetMaxDomainEventsBetweenFlush(int value)
        {
            MaxDomainEventsBetweenFlushSet(value);
        }
    }
}
using System;
using System.Collections.Generic;
using d60.Cirqus.Events;
using d60.Cirqus.Views.ViewManagers;

namespace d60.Cirqus.EntityFramework
{
    public class EntityFrameworkViewManager<TViewInstance> : AbstractViewManager<TViewInstance> where TViewInstance : class, IViewInstance, ISubscribeTo, new()
    {
        readonly string _connectionString;

        public EntityFrameworkViewManager(string connectionString)
        {
            _connectionString = connectionString;
        }

        public override long GetPosition(bool canGetFromCache = true)
        {
            throw new NotImplementedException();
        }

        public override void Dispatch(IViewContext viewContext, IEnumerable<DomainEvent> batch)
        {
            throw new NotImplementedException();
        }

        public override void Purge()
        {
            throw new NotImplementedException();
        }

        public override TViewInstance Load(string viewId)
        {
            throw new NotImplementedException();
        }
    }
}
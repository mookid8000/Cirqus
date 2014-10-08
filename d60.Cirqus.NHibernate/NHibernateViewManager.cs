using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using d60.Cirqus.Events;
using d60.Cirqus.Views.ViewManagers;

namespace d60.Cirqus.NHibernate
{
    public class NHibernateViewManager<TViewInstance> : IViewManager<TViewInstance> where TViewInstance : class, IViewInstance, ISubscribeTo, new()
    {
        readonly string _connectionString;

        public NHibernateViewManager(string connectionStringOrConnectionStringName)
        {
            _connectionString = SqlHelper.GetConnectionString(connectionStringOrConnectionStringName);
            
        }
        public long GetPosition(bool canGetFromCache = true)
        {
            throw new NotImplementedException();
        }

        public void Dispatch(IViewContext viewContext, IEnumerable<DomainEvent> batch)
        {
            throw new NotImplementedException();
        }

        public Task WaitUntilProcessed(CommandProcessingResult result, TimeSpan timeout)
        {
            throw new NotImplementedException();
        }

        public void Purge()
        {
            throw new NotImplementedException();
        }

        public TViewInstance Load(string viewId)
        {
            throw new NotImplementedException();
        }

        public event ViewInstanceUpdatedHandler<TViewInstance> Updated;
    }
}

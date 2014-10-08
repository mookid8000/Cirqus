using System.Collections.Generic;
using System.Linq;
using d60.Cirqus.Events;
using d60.Cirqus.Views.ViewManagers;
using FluentNHibernate.Automapping;
using FluentNHibernate.Cfg;
using FluentNHibernate.Cfg.Db;
using NHibernate;
using NHibernate.Cfg;

namespace d60.Cirqus.NHibernate
{
    public class NHibernateViewManager<TViewInstance> : AbstractViewManager<TViewInstance> where TViewInstance : class, IViewInstance, ISubscribeTo, new()
    {
        readonly Configuration _configuration;
        readonly ISessionFactory _sessionFactory;

        public NHibernateViewManager(string connectionStringOrConnectionStringName)
        {
            var connectionString = SqlHelper.GetConnectionString(connectionStringOrConnectionStringName);

            _configuration = Fluently.Configure()
                .Database(MsSqlConfiguration.MsSql2012)
                .Mappings(m =>
                {
                    var automappingConfiguration = new DefaultAutomappingConfiguration();
                    var model = new AutoPersistenceModel(automappingConfiguration);

                    model.Add(typeof (Position));

                    m.AutoMappings.Add(model);
                })
                .BuildConfiguration();
            
            _configuration.Properties["connection.connection_string"] = connectionString;

            _sessionFactory = _configuration.BuildSessionFactory();
        }

        public override long GetPosition(bool canGetFromCache = true)
        {
            using (var session = _sessionFactory.OpenSession())
            {
                var currentPosition = session
                    .QueryOver<Position>()
                    .Where(p => p.Id == typeof(TViewInstance).Name)
                    .List()
                    .FirstOrDefault();

                if (currentPosition == null)
                    return -1;

                return currentPosition.CurrentPosition;
            }
        }

        public override void Dispatch(IViewContext viewContext, IEnumerable<DomainEvent> batch)
        {
         //   throw new NotImplementedException();
        }

        public override void Purge()
        {
            //throw new NotImplementedException();
        }

        public override TViewInstance Load(string viewId)
        {
//            throw new NotImplementedException();

            return null;
        }
    }

    class Position
    {
        public virtual string Id { get; set; }
     
        public virtual long CurrentPosition { get; set; }
    }
}

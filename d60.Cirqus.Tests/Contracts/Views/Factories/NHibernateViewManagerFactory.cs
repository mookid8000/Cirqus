using d60.Cirqus.NHibernate;
using d60.Cirqus.Tests.MsSql;
using d60.Cirqus.Views.ViewManagers;

namespace d60.Cirqus.Tests.Contracts.Views.Factories
{
    public class NHibernateViewManagerFactory : AbstractViewManagerFactory
    {
        readonly string _connectionString;

        public NHibernateViewManagerFactory()
        {
            MsSqlTestHelper.EnsureTestDatabaseExists();

            _connectionString = MsSqlTestHelper.ConnectionString;
        }

        protected override IViewManager<TViewInstance> CreateViewManager<TViewInstance>()
        {
            return new NHibernateViewManager<TViewInstance>(_connectionString);
        }
    }
}
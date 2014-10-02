using d60.Cirqus.EntityFramework;
using d60.Cirqus.Tests.MsSql;
using d60.Cirqus.Views.ViewManagers;

namespace d60.Cirqus.Tests.Contracts.Views.Factories
{
    public class EntityFrameworkViewManagerFactory : AbstractViewManagerFactory
    {
        readonly string _connectionString;

        public EntityFrameworkViewManagerFactory()
        {
            MsSqlTestHelper.EnsureTestDatabaseExists();

            _connectionString = MsSqlTestHelper.ConnectionString;
        }

        protected override IViewManager<TViewInstance> CreateViewManager<TViewInstance>()
        {
            var tableName = typeof(TViewInstance).Name;

            MsSqlTestHelper.DropTable(tableName);
            
            return new EntityFrameworkViewManager<TViewInstance>(_connectionString);
        }
    }
}
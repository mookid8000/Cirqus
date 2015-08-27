using d60.Cirqus.MsSql.Views;
using d60.Cirqus.Tests.MsSql;
using d60.Cirqus.Views.ViewManagers;

namespace d60.Cirqus.Tests.Contracts.Views.Factories
{
    public class MsSqlViewManagerFactory : AbstractViewManagerFactory
    {
        readonly string _connectionString;

        public MsSqlViewManagerFactory()
        {
            MsSqlTestHelper.EnsureTestDatabaseExists();

            _connectionString = MsSqlTestHelper.ConnectionString;
        }

        protected override IViewManager<TViewInstance> CreateViewManager<TViewInstance>(bool enableBatchDispatch = false)
        {
            var tableName = typeof(TViewInstance).Name;

            MsSqlTestHelper.DropTable(tableName);

            var viewManager = new MsSqlViewManager<TViewInstance>(_connectionString, tableName)
            {
                BatchDispatchEnabled = enableBatchDispatch
            };

            return viewManager;
        }
    }
}
using d60.Cirqus.MsSql.Views;
using d60.Cirqus.Tests.MsSql;
using d60.Cirqus.Views.ViewManagers.New;

namespace d60.Cirqus.Tests.Contracts.Views.New.Factories
{
    public class MsSqlManagedViewFactory : AbstractManagedViewFactory
    {
        readonly string _connectionString;

        public MsSqlManagedViewFactory()
        {
            MsSqlTestHelper.EnsureTestDatabaseExists();

            _connectionString = MsSqlTestHelper.ConnectionString;
        }

        protected override IManagedView<TViewInstance> CreateManagedView<TViewInstance>()
        {
            var tableName = typeof(TViewInstance).Name;

            MsSqlTestHelper.DropTable(tableName);

            var viewManager = new NewMsSqlViewManager<TViewInstance>(_connectionString, tableName);

            return viewManager;
        }
    }
}
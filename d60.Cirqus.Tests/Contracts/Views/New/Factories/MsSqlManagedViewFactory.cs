using d60.Cirqus.MsSql.Views;
using d60.Cirqus.Tests.MsSql;
using d60.Cirqus.Views.ViewManagers.New;

namespace d60.Cirqus.Tests.Contracts.Views.New.Factories
{
    public class MsSqlManagedViewFactory : ManagedViewFactoryBase
    {
        readonly string _connectionString;

        public MsSqlManagedViewFactory()
        {
            MsSqlTestHelper.EnsureTestDatabaseExists();

            _connectionString = MsSqlTestHelper.ConnectionString;
        }

        protected override IManagedView<TViewInstance> CreateManagedView<TViewInstance>()
        {
            var viewManager = new NewMsSqlViewManager<TViewInstance>(_connectionString);

            return viewManager;
        }
    }
}
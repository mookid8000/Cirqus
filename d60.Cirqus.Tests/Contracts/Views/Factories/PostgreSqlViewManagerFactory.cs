using d60.Cirqus.PostgreSql.Views;
using d60.Cirqus.Tests.PostgreSql;
using d60.Cirqus.Views.ViewManagers;

namespace d60.Cirqus.Tests.Contracts.Views.Factories
{
    public class PostgreSqlViewManagerFactory : AbstractViewManagerFactory
    {
        readonly string _connectionString;

        public PostgreSqlViewManagerFactory()
        {
            _connectionString = PostgreSqlTestHelper.PostgreSqlConnectionString;
        }

        protected override IViewManager<TViewInstance> CreateViewManager<TViewInstance>()
        {
            var tableName = typeof(TViewInstance).Name;

            PostgreSqlTestHelper.DropTable(tableName);

            return new PostgreSqlViewManager<TViewInstance>(_connectionString, tableName);
        }
    }
}
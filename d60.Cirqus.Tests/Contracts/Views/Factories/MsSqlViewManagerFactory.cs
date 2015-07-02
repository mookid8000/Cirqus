using d60.Cirqus.HybridDb;
using d60.Cirqus.MsSql.Views;
using d60.Cirqus.Tests.MsSql;
using d60.Cirqus.Views.ViewManagers;
using HybridDb;
using HybridDb.Migrations;

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

        protected override IViewManager<TViewInstance> CreateViewManager<TViewInstance>()
        {
            var tableName = typeof(TViewInstance).Name;

            MsSqlTestHelper.DropTable(tableName);

            var viewManager = new MsSqlViewManager<TViewInstance>(_connectionString, tableName);

            return viewManager;
        }
    }    
    
    public class HybridDbViewManagerFactory : AbstractViewManagerFactory
    {
        readonly IDocumentStore store;

        public HybridDbViewManagerFactory()
        {
            store = RegisterDisposable(DocumentStore.ForTesting(TableMode.UseRealTables, "Server=localhost;Initial Catalog=cirqus_hybrid_test;Integrated Security=True", new NullHybridDbConfigurator()));
        }

        protected override IViewManager<TViewInstance> CreateViewManager<TViewInstance>()
        {
            store.Configuration.Document<TViewInstance>();
            new SchemaMigrationRunner(store, new SchemaDiffer()).Run();
            return new HybridDbViewManager<TViewInstance>(store);
        }
    }
}
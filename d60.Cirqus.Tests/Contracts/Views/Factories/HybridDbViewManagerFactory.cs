using d60.Cirqus.HybridDb;
using d60.Cirqus.Tests.MsSql;
using d60.Cirqus.Views.ViewManagers;
using HybridDb;

namespace d60.Cirqus.Tests.Contracts.Views.Factories
{
    public class HybridDbViewManagerFactory : AbstractViewManagerFactory
    {
        public HybridDbViewManagerFactory()
        {
            MsSqlTestHelper.EnsureTestDatabaseExists();
            MsSqlTestHelper.DropTable("HybridDb");
            MsSqlTestHelper.DropTable("ViewPosition");
        }

        protected override IViewManager<TViewInstance> CreateViewManager<TViewInstance>()
        {
            var tableName = typeof (TViewInstance) + "_HybridDb";

            MsSqlTestHelper.DropTable(tableName);

            var documentStore = DocumentStore.ForTesting(
                    TableMode.UseRealTables,
                    MsSqlTestHelper.ConnectionString,
                    new LambdaHybridDbConfigurator(x =>
                    {
                        x.Document<ViewPosition>().With("Id", v => v.Id);
                        x.Document<TViewInstance>(tableName).With("Id", v => v.Id);
                    }));

            RegisterDisposable(documentStore);

            return new HybridDbViewManager<TViewInstance>(documentStore);
        }
    }
}
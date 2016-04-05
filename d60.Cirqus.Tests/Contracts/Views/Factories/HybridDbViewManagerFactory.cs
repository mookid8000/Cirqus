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

        protected override IViewManager<TViewInstance> CreateViewManager<TViewInstance>(bool enableBatchDispatch = false)
        {
            var tableName = typeof (TViewInstance).Name + "_HybridDb";

            MsSqlTestHelper.DropTable(tableName);

            var documentStore = DocumentStore.ForTesting(
                    TableMode.UseRealTables,
                    MsSqlTestHelper.ConnectionString,
                    x =>
                    {
                        x.Document<ViewPosition>().Key(v => v.Id);
                        x.Document<TViewInstance>(tableName).Key(v => v.Id);
                    });

            documentStore.Initialize();

            RegisterDisposable(documentStore);

            return new HybridDbViewManager<TViewInstance>(documentStore)
            {
                BatchDispatchEnabled = enableBatchDispatch
            };
        }
    }
}
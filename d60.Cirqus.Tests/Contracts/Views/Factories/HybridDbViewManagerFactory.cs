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
        }

        protected override IViewManager<TViewInstance> CreateViewManager<TViewInstance>(bool enableBatchDispatch = false)
        {
            MsSqlTestHelper.DropTable(typeof(TViewInstance).Name + "_HybridDb");
            MsSqlTestHelper.DropTable(typeof(TViewInstance).Name + "_Documents");
            MsSqlTestHelper.DropTable(typeof(TViewInstance).Name + "_ViewPositions");
            MsSqlTestHelper.DropTable(typeof(TViewInstance).Name + "_Views");

            var documentStore = DocumentStore.ForTesting(
                    TableMode.UseRealTables,
                    MsSqlTestHelper.ConnectionString,
                    x =>
                    {
                        x.UseTableNamePrefix(typeof(TViewInstance).Name + "_");
                        x.Document<ViewPosition>().Key(v => v.Id);
                        x.Document<TViewInstance>("Views").Key(v => v.Id);
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
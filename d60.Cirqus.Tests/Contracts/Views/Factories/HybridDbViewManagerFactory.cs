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
        }

        protected override IViewManager<TViewInstance> CreateViewManager<TViewInstance>()
        {
            var documentStore = DocumentStore.ForTesting(
                    TableMode.UseRealTables,
                    MsSqlTestHelper.ConnectionString,
                    new LambdaHybridDbConfigurator(x =>
                    {
                        x.Document<HybridDbViewManager<TViewInstance>.ViewPosition>().With("Id", v => v.Id);
                        x.Document<TViewInstance>().With("Id", v => v.Id);
                    }));

            RegisterDisposable(documentStore);

            return new HybridDbViewManager<TViewInstance>(documentStore);
        }
    }
}
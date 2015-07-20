using d60.Cirqus.HybridDb;
using d60.Cirqus.Views.ViewManagers;
using HybridDb;
using HybridDb.Migrations;

namespace d60.Cirqus.Tests.Contracts.Views.Factories
{
    public class HybridDbViewManagerFactory : AbstractViewManagerFactory
    {
        readonly IDocumentStore store;

        public HybridDbViewManagerFactory()
        {
            store = RegisterDisposable(
                DocumentStore.ForTesting(
                    TableMode.UseRealTables,
                    "Server=localhost;Initial Catalog=cirqus_hybrid_test;Integrated Security=True",
                    new LambdaHybridDbConfigurator(x =>
                    {
                        x.Document<IViewInstance>().With("Id", v => v.Id);
                    })));
        }

        protected override IViewManager<TViewInstance> CreateViewManager<TViewInstance>()
        {
            store.Configuration.Document<TViewInstance>();
            new SchemaMigrationRunner(store, new SchemaDiffer()).Run();
            return new HybridDbViewManager<TViewInstance>(store);
        }
    }
}
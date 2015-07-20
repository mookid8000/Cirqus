using d60.Cirqus.HybridDb;
using d60.Cirqus.Views.ViewManagers;
using HybridDb;

namespace d60.Cirqus.Tests.Contracts.Views.Factories
{
    public class HybridDbViewManagerFactory : AbstractViewManagerFactory
    {
        IDocumentStore store;

        protected override IViewManager<TViewInstance> CreateViewManager<TViewInstance>()
        {
            store = RegisterDisposable(
                DocumentStore.ForTesting(
                    TableMode.UseRealTables,
                    "Server=localhost;Initial Catalog=cirqus_hybrid_test;Integrated Security=True",
                    new LambdaHybridDbConfigurator(x =>
                    {
                        x.Document<HybridDbViewManager<TViewInstance>.ViewPosition>().With("Id", v => v.Id);
                        x.Document<TViewInstance>().With("Id", v => v.Id);
                    })));

            return new HybridDbViewManager<TViewInstance>(store);
        }
    }
}
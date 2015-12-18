using System.Threading.Tasks;
using d60.Cirqus.Aggregates;
using d60.Cirqus.Config;
using d60.Cirqus.Config.Configurers;
using d60.Cirqus.Events;
using d60.Cirqus.Testing.Internals;
using d60.Cirqus.Tests.Stubs;
using d60.Cirqus.Views;

namespace d60.Cirqus.Tests.Extensions
{
    public static class Helpful
    {
        public static TAggregateRoot Get<TAggregateRoot>(this IAggregateRootRepository repo, string aggregateRootId) where TAggregateRoot : AggregateRoot, new()
        {
            var aggregateRoot = repo.Get<TAggregateRoot>(aggregateRootId, new InMemoryUnitOfWork(repo, new DefaultDomainTypeNameMapper()), createIfNotExists: true);
            return (TAggregateRoot)aggregateRoot;
        }

        internal static Task<InMemoryEventStore> UseInMemoryEventStore(this EventStoreConfigurationBuilder builder)
        {
            var taskCompletionSource = new TaskCompletionSource<InMemoryEventStore>();
            
            builder.Register<IEventStore>(c =>
            {
                var inMemoryEventStore = new InMemoryEventStore();
                taskCompletionSource.SetResult(inMemoryEventStore);
                return inMemoryEventStore;
            });

            return taskCompletionSource.Task;
        }

        internal static void UseConsoleOutEventDispatcher(this EventDispatcherConfigurationBuilder builder)
        {
            builder.UseEventDispatcher(c => new ConsoleOutEventDispatcher(c.Get<IEventStore>()));
        }
    }
}
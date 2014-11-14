using System.Threading.Tasks;
using d60.Cirqus.Aggregates;
using d60.Cirqus.Config;
using d60.Cirqus.Config.Configurers;
using d60.Cirqus.Events;
using d60.Cirqus.Serialization;
using d60.Cirqus.Testing.Internals;
using d60.Cirqus.Tests.Stubs;
using d60.Cirqus.Views;

namespace d60.Cirqus.Tests.Extensions
{
    public static class Helpful
    {
        public static AggregateRootInfo<TAggregateRoot> Get<TAggregateRoot>(this IAggregateRootRepository repo, string aggregateRootId) where TAggregateRoot : AggregateRoot, new()
        {
            return repo.Get<TAggregateRoot>(aggregateRootId, new InMemoryUnitOfWork(repo, new DefaultDomainTypeNameMapper()), createIfNotExists: true);
        }

        internal static Task<InMemoryEventStore> UseInMemoryEventStore(this EventStoreConfigurationBuilder builder)
        {
            var taskCompletionSource = new TaskCompletionSource<InMemoryEventStore>();
            
            builder.Registrar.Register<IEventStore>(c =>
            {
                var inMemoryEventStore = new InMemoryEventStore(c.Get<IDomainEventSerializer>());
                taskCompletionSource.SetResult(inMemoryEventStore);
                return inMemoryEventStore;
            });

            return taskCompletionSource.Task;
        }

        internal static void UseConsoleOutEventDispatcher(this EventDispatcherConfigurationBuilder builder)
        {
            builder.Registrar.RegisterInstance<IEventDispatcher>(new ConsoleOutEventDispatcher());
        }
    }
}
using d60.Cirqus.Aggregates;
using d60.Cirqus.Testing.Internals;

namespace d60.Cirqus.Tests.Extensions
{
    public static class Helpful
    {
        public static AggregateRootInfo<TAggregateRoot> Get<TAggregateRoot>(this IAggregateRootRepository repo, string aggregateRootId) where TAggregateRoot : AggregateRoot, new()
        {
            return repo.Get<TAggregateRoot>(aggregateRootId, new InMemoryUnitOfWork(repo), createIfNotExists: true);
        }
    }
}
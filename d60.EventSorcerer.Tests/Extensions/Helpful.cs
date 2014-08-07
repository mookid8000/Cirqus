using System;
using d60.EventSorcerer.Aggregates;
using d60.EventSorcerer.TestHelpers.Internals;

namespace d60.EventSorcerer.Tests.Extensions
{
    public static class Helpful
    {
        public static AggregateRootInfo<TAggregateRoot> Get<TAggregateRoot>(this IAggregateRootRepository repo, Guid aggregateRootId) where TAggregateRoot : AggregateRoot, new()
        {
            return repo.Get<TAggregateRoot>(aggregateRootId, new InMemoryUnitOfWork());
        }
    }
}
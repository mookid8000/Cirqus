using System;
using d60.Cirqus.Aggregates;

namespace d60.Cirqus.Commands
{
    public interface ICommandContext
    {
        TAggregateRoot Load<TAggregateRoot>(Guid aggregateRootId) where TAggregateRoot : AggregateRoot, new();
    }
}
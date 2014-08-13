using System;
using d60.Cirqus.Aggregates;

namespace d60.Cirqus.Views.ViewManagers
{
    public interface IViewContext
    {
        TAggregateRoot Load<TAggregateRoot>(Guid aggregateRootId, long globalSequenceNumber) where TAggregateRoot : AggregateRoot, new();
    }
}
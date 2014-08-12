using System;
using d60.Circus.Aggregates;

namespace d60.Circus.Views.Basic
{
    public interface IViewContext
    {
        TAggregateRoot Load<TAggregateRoot>(Guid aggregateRootId, long globalSequenceNumber) where TAggregateRoot : AggregateRoot, new();
    }
}
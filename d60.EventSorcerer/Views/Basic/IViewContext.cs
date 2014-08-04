using System;
using d60.EventSorcerer.Aggregates;

namespace d60.EventSorcerer.Views.Basic
{
    public interface IViewContext
    {
        TAggregateRoot Load<TAggregateRoot>(Guid aggregateRootId, long globalSequenceNumber) where TAggregateRoot : AggregateRoot, new();
    }
}
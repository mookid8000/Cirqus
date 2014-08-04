using System;

namespace d60.EventSorcerer.Views.Basic
{
    public delegate object AggregateRootResolver(Type aggregateRootType, Guid aggregateRootId, long globalSequenceNumber);
}
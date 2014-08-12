using System;

namespace d60.Circus.Views.Basic
{
    public delegate object AggregateRootResolver(Type aggregateRootType, Guid aggregateRootId, long globalSequenceNumber);
}
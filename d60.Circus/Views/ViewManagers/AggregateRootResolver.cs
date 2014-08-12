using System;

namespace d60.Circus.Views.ViewManagers
{
    public delegate object AggregateRootResolver(Type aggregateRootType, Guid aggregateRootId, long globalSequenceNumber);
}
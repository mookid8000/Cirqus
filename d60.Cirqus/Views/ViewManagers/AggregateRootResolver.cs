using System;

namespace d60.Cirqus.Views.ViewManagers
{
    public delegate object AggregateRootResolver(Type aggregateRootType, Guid aggregateRootId, long globalSequenceNumber);
}
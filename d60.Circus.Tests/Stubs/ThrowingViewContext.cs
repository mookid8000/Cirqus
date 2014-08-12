using System;
using d60.Circus.Aggregates;
using d60.Circus.Views.Basic;

namespace d60.Circus.Tests.Stubs
{
    public class ThrowingViewContext : IViewContext
    {
        public TAggregateRoot Load<TAggregateRoot>(Guid aggregateRootId, long globalSequenceNumber) where TAggregateRoot : AggregateRoot, new()
        {
            throw new NotImplementedException("This view context is a stub that throws when someone uses it");
        }
    }
}
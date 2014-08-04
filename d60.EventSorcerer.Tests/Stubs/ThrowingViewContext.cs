using System;
using d60.EventSorcerer.Aggregates;
using d60.EventSorcerer.Views.Basic;

namespace d60.EventSorcerer.Tests.Stubs
{
    public class ThrowingViewContext : IViewContext
    {
        public TAggregateRoot Load<TAggregateRoot>(Guid aggregateRootId, long globalSequenceNumber) where TAggregateRoot : AggregateRoot, new()
        {
            throw new NotImplementedException("This view context is a stub that throws when someone uses it");
        }
    }
}
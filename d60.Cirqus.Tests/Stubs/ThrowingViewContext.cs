using System;
using d60.Cirqus.Aggregates;
using d60.Cirqus.Events;
using d60.Cirqus.Views.ViewManagers;

namespace d60.Cirqus.Tests.Stubs
{
    public class ThrowingViewContext : IViewContext
    {
        public TAggregateRoot Load<TAggregateRoot>(Guid aggregateRootId) where TAggregateRoot : AggregateRoot, new()
        {
            throw new NotImplementedException("This view context is a stub that throws when someone uses it");
        }

        public TAggregateRoot Load<TAggregateRoot>(Guid aggregateRootId, long globalSequenceNumber) where TAggregateRoot : AggregateRoot, new()
        {
            throw new NotImplementedException("This view context is a stub that throws when someone uses it");
        }

        /// <summary>
        /// We do allow for setting/getting the event currently being handled though
        /// </summary>
        public DomainEvent CurrentEvent { get; set; }
    }
}
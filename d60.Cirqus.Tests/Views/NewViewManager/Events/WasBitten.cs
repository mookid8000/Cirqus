using d60.Cirqus.Events;
using d60.Cirqus.Tests.Views.NewViewManager.AggregateRoots;

namespace d60.Cirqus.Tests.Views.NewViewManager.Events
{
    public class WasBitten : DomainEvent<Potato>
    {
        public decimal FractionBittenOff { get; set; }
    }
}
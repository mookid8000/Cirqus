using d60.Cirqus.Events;
using d60.Cirqus.Tests.Projections.Views.NewViewManager.AggregateRoots;

namespace d60.Cirqus.Tests.Projections.Views.NewViewManager.Events
{
    public class BaptizedSomeone : DomainEvent<John>
    {
        public int NameIndex { get; set; }
    }
}
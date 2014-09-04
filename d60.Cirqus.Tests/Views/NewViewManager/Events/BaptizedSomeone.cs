using d60.Cirqus.Events;
using d60.Cirqus.Tests.Views.NewViewManager.AggregateRoots;

namespace d60.Cirqus.Tests.Views.NewViewManager.Events
{
    public class BaptizedSomeone : DomainEvent<John>
    {
        public int NameIndex { get; set; }
    }
}
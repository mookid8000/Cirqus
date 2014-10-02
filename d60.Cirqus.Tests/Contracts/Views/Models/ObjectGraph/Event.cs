using d60.Cirqus.Events;

namespace d60.Cirqus.Tests.Contracts.Views.Models.ObjectGraph
{
    public class Event : DomainEvent<Root>
    {
        public int NumberOfChildren { get; set; }
    }
}
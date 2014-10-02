using d60.Cirqus.Events;

namespace d60.Cirqus.Tests.Contracts.Views.Models.RecoveryTest
{
    public class Event : DomainEvent<Root>
    {
        public int EventId { get; set; }
    }
}
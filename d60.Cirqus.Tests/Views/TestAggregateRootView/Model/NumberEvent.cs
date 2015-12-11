using d60.Cirqus.Events;

namespace d60.Cirqus.Tests.Views.TestAggregateRootView.Model
{
    public class NumberEvent : DomainEvent<AggregateRootWithLogic>
    {
        public int Counter { get; private set; }

        public NumberEvent(int counter)
        {
            Counter = counter;
        }
    }
}
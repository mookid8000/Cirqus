using d60.Cirqus.Aggregates;
using d60.Cirqus.Events;

namespace d60.Cirqus.Tests.Views.TestAggregateRootView.Model
{
    public class AggregateRootWithLogic : AggregateRoot, IEmit<NumberEvent>
    {
        int _counter;

        public void Increment()
        {
            Emit(new NumberEvent(_counter + 1));
        }

        public void Apply(NumberEvent e)
        {
            _counter = e.Counter;
        }

        public int Counter
        {
            get { return _counter; }
        }
    }
}
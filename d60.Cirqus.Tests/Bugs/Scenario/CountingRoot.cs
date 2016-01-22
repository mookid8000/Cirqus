using d60.Cirqus.Aggregates;
using d60.Cirqus.Events;

namespace d60.Cirqus.Tests.Bugs.Scenario
{
    public class CountingRoot : AggregateRoot, IEmit<CountingRootIncremented>
    {
        int _number;

        public int Number => _number;

        public void IncrementYourself()
        {
            Emit(new CountingRootIncremented(_number + 1));
        }

        public void Apply(CountingRootIncremented e)
        {
            _number = e.NytTal;
        }
    }
}
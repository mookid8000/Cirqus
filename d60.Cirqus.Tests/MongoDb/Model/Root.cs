using d60.Cirqus.Aggregates;
using d60.Cirqus.Events;

namespace d60.Cirqus.Tests.MongoDb.Model
{
    public class Root : AggregateRoot, IEmit<RootIncrementedTo>
    {
        int _myNumber;

        public void IncrementYourself()
        {
            Emit(new RootIncrementedTo(_myNumber + 1));
        }

        public void Apply(RootIncrementedTo e)
        {
            _myNumber = e.Number;
        }
    }
}
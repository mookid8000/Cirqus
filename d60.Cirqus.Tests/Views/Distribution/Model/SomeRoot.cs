using d60.Cirqus.Aggregates;
using d60.Cirqus.Events;

namespace d60.Cirqus.Tests.Views.Distribution.Model
{
    public class SomeRoot : AggregateRoot, IEmit<DidStuff>
    {
        public void DoStuff()
        {
            Emit(new DidStuff());
        }

        public void Apply(DidStuff e)
        {
            
        }
    }
}
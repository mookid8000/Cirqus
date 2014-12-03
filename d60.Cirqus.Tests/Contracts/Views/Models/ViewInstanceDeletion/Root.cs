using d60.Cirqus.Aggregates;
using d60.Cirqus.Events;

namespace d60.Cirqus.Tests.Contracts.Views.Models.ViewInstanceDeletion
{
    public class Root : AggregateRoot, IEmit<SomethingHappened>, IEmit<Undone>
    {
        public void MakeStuffHappen()
        {
            Emit(new SomethingHappened());
        }

        public void Apply(SomethingHappened e)
        {
        }

        public void Undo()
        {
            Emit(new Undone());
        }

        public void Apply(Undone e)
        {
            
        }
    }
}
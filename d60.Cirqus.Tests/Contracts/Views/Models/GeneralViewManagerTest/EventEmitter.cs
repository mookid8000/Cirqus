using d60.Cirqus.Aggregates;
using d60.Cirqus.Events;

namespace d60.Cirqus.Tests.Contracts.Views.Models.GeneralViewManagerTest
{
    public class EventEmitter : AggregateRoot, IEmit<AnEvent>
    {
        public void Apply(AnEvent e)
        {
        }

        public void DoIt()
        {
            Emit(new AnEvent());
        }
    }
}
using d60.Cirqus.Commands;

namespace d60.Cirqus.Tests.Views.TestAggregateRootView.Model
{
    public class IncrementNumberCommand : Command<AggregateRootWithLogic>
    {
        public IncrementNumberCommand(string aggregateRootId) : base(aggregateRootId)
        {
        }

        public override void Execute(AggregateRootWithLogic aggregateRoot)
        {
            aggregateRoot.Increment();
        }
    }
}
using d60.Cirqus.Commands;

namespace d60.Cirqus.Tests.Contracts.Views.Models.ViewInstanceDeletion
{
    public class Undo : Command<Root>
    {
        public Undo(string aggregateRootId) : base(aggregateRootId)
        {
        }

        public override void Execute(Root aggregateRoot)
        {
            aggregateRoot.Undo();
        }
    }
}
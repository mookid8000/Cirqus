using d60.Cirqus.Commands;

namespace d60.Cirqus.Tests.Contracts.Views.Models.ViewInstanceDeletion
{
    public class MakeStuffHappen : Command<Root>
    {
        public MakeStuffHappen(string aggregateRootId) : base(aggregateRootId)
        {
        }

        public override void Execute(Root aggregateRoot)
        {
            aggregateRoot.MakeStuffHappen();
        }
    }
}
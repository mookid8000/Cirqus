using d60.Cirqus.Commands;

namespace d60.Cirqus.Tests.MongoDb.Model
{
    public class IncrementYourself : Command<Root>
    {
        public IncrementYourself(string aggregateRootId) : base(aggregateRootId)
        {
        }

        public override void Execute(Root aggregateRoot)
        {
            aggregateRoot.IncrementYourself();
        }
    }
}
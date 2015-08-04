using d60.Cirqus.Commands;

namespace d60.Cirqus.Tests.Views.Distribution.Model
{
    public class MakeSomeRootDoStuff : Command<SomeRoot>
    {
        public MakeSomeRootDoStuff(string aggregateRootId) : base(aggregateRootId)
        {
        }

        public override void Execute(SomeRoot aggregateRoot)
        {
            aggregateRoot.DoStuff();
        }
    }
}
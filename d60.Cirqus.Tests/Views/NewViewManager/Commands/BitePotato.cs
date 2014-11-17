using d60.Cirqus.Commands;
using d60.Cirqus.Tests.Views.NewViewManager.AggregateRoots;

namespace d60.Cirqus.Tests.Views.NewViewManager.Commands
{
    public class BitePotato : ExecutableCommand
    {
        public BitePotato(string potatoId, decimal fractionToBiteOff)
        {
            PotatoId = potatoId;
            FractionToBiteOff = fractionToBiteOff;
        }

        public string PotatoId { get; private set; }

        public decimal FractionToBiteOff { get; private set; }

        public override void Execute(ICommandContext context)
        {
            var potato = context.TryLoad<Potato>(PotatoId)
                         ?? context.Create<Potato>(PotatoId);
            
            potato.Bite(FractionToBiteOff);
        }
    }
}
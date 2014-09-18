using System;
using d60.Cirqus.Commands;
using d60.Cirqus.Tests.Projections.Views.NewViewManager.AggregateRoots;

namespace d60.Cirqus.Tests.Projections.Views.NewViewManager.Commands
{
    public class BitePotato : Command
    {
        public BitePotato(Guid potatoId, decimal fractionToBiteOff)
        {
            PotatoId = potatoId;
            FractionToBiteOff = fractionToBiteOff;
        }

        public Guid PotatoId { get; private set; }

        public decimal FractionToBiteOff { get; private set; }

        public override void Execute(ICommandContext context)
        {
            context.Load<Potato>(PotatoId).Bite(FractionToBiteOff);
        }
    }
}
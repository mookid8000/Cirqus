using System;
using d60.Cirqus.Commands;
using d60.Cirqus.Tests.Views.NewViewManager.AggregateRoots;

namespace d60.Cirqus.Tests.Views.NewViewManager.Commands
{
    public class BitePotato : Command
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
            context.Load<Potato>(PotatoId).Bite(FractionToBiteOff);
        }
    }
}
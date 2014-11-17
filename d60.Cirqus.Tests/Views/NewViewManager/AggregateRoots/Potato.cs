using d60.Cirqus.Aggregates;
using d60.Cirqus.Events;
using d60.Cirqus.Tests.Views.NewViewManager.Events;

namespace d60.Cirqus.Tests.Views.NewViewManager.AggregateRoots
{
    public class Potato : AggregateRoot, IEmit<WasBitten>, IEmit<WasEaten>, IEmit<PotatoCreated>
    {
        decimal _remainingFraction = 1;

        public void Bite(decimal fractionToBiteOff)
        {
            if (_remainingFraction == 0) return;

            if (_remainingFraction - fractionToBiteOff <= 0)
            {
                Emit(new WasEaten());
                return;
            }

            Emit(new WasBitten { FractionBittenOff = fractionToBiteOff });
        }

        protected override void Created()
        {
            var john = TryLoad<John>(John.TheJohnId) ?? Create<John>(John.TheJohnId);

            Emit(new PotatoCreated { Name = john.GetNextName() });
        }

        public void Apply(WasBitten e)
        {
            _remainingFraction -= e.FractionBittenOff;
        }

        public void Apply(WasEaten e)
        {
            _remainingFraction = 0;
        }

        public void Apply(PotatoCreated e)
        {
        }
    }
}
using System;
using d60.Cirqus.Aggregates;
using d60.Cirqus.Events;
using d60.Cirqus.Tests.Projections.Views.NewViewManager.Events;

namespace d60.Cirqus.Tests.Projections.Views.NewViewManager.AggregateRoots
{
    public class John : AggregateRoot, IEmit<BaptizedSomeone>
    {
        public static readonly Guid Id = new Guid("97E939E3-E84F-49DB-8856-1740A915F784");

        readonly string[] _names = { "Jeff", "Bunny", "Walter", "Donny" };

        int _nextNameIndex;

        public string GetNextName()
        {
            var name = _names[_nextNameIndex];

            Emit(new BaptizedSomeone
            {
                NameIndex = (_nextNameIndex + 1) % _names.Length
            });

            return name;
        }

        public void Apply(BaptizedSomeone e)
        {
            _nextNameIndex = e.NameIndex;
        }
    }
}
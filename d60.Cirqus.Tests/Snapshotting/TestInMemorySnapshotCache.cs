using System;
using System.Collections.Generic;
using System.Linq;
using d60.Cirqus.Aggregates;
using d60.Cirqus.Snapshotting;
using NUnit.Framework;

namespace d60.Cirqus.Tests.Snapshotting
{
    [TestFixture]
    public class TestInMemorySnapshotCache : FixtureBase
    {
        [Test]
        public void CanCloneDeepAndGood()
        {
            var id = Guid.NewGuid();
            var instance = new SomeRootWithVariousDifficultThingsGoingOnForIt {Id = id};
            Console.WriteLine(instance.GetHashCode());

            var cache = new InMemorySnapshotCache();
            cache.PutCloneToCache(AggregateRootInfo<SomeRootWithVariousDifficultThingsGoingOnForIt>.Old(instance, 0, 0));

            var rootInfo = cache.GetCloneFromCache<SomeRootWithVariousDifficultThingsGoingOnForIt>(id, 0);
            var frozenInstance = rootInfo.AggregateRoot;

            Assert.That(frozenInstance.GetHashCode(), Is.EqualTo(instance.GetHashCode()));
        }


        public class SomeRootWithVariousDifficultThingsGoingOnForIt : AggregateRoot
        {
            readonly List<ThisBadBoyIsNestedAndHasPropertyWithPrivateSetter> _nestedBadBoys;

            readonly IEnumerable<ThisOneHasPrivateFields> _enumerableOfStuff;

            readonly HashSet<ThisOneIsAbstract> _abstracts;

            public SomeRootWithVariousDifficultThingsGoingOnForIt()
            {
                _nestedBadBoys = new List<ThisBadBoyIsNestedAndHasPropertyWithPrivateSetter>
                {
                    new ThisBadBoyIsNestedAndHasPropertyWithPrivateSetter(1),
                    new ThisBadBoyIsNestedAndHasPropertyWithPrivateSetter(2),
                    new ThisBadBoyIsNestedAndHasPropertyWithPrivateSetter(3),
                };

                _enumerableOfStuff = new List<ThisOneHasPrivateFields>
                {
                    new ThisOneHasPrivateFields("private1"),
                    new ThisOneHasPrivateFields("private2"),
                };

                _abstracts = new HashSet<ThisOneIsAbstract>
                {
                    new ThisIsTheFirstImplementation(),
                    new ThisIsTheSecondImplementation()
                };
            }

            public override int GetHashCode()
            {
                return Id.GetHashCode()

                       ^

                       _nestedBadBoys
                           .Aggregate(_nestedBadBoys.Count.GetHashCode(),
                               (value, badBoy) => value ^ badBoy.GetHashCode())

                       ^

                       _enumerableOfStuff
                           .Aggregate(_enumerableOfStuff.Count().GetHashCode(),
                               (value, stuff) => value ^ stuff.GetHashCode())

                       ^

                       _abstracts
                           .Aggregate(_abstracts.Count.GetHashCode(),
                               (value, abs) => value ^ abs.GetHashCode());
            }
        }

        public class ThisBadBoyIsNestedAndHasPropertyWithPrivateSetter
        {
            public ThisBadBoyIsNestedAndHasPropertyWithPrivateSetter(int number)
            {
                Number = number;
            }

            public int Number { get; private set; }

            public override int GetHashCode()
            {
                return Number.GetHashCode();
            }
        }

        public class ThisOneHasPrivateFields
        {
            readonly string _name;

            public ThisOneHasPrivateFields(string name)
            {
                _name = name;
            }

            public override int GetHashCode()
            {
                return _name.GetHashCode();
            }
        }

        public abstract class ThisOneIsAbstract
        {
            public abstract string WhoAreYou();
        }

        public class ThisIsTheFirstImplementation : ThisOneIsAbstract
        {
            public override string WhoAreYou()
            {
                return "first";
            }

            public override int GetHashCode()
            {
                return "first".GetHashCode();
            }
        }

        public class ThisIsTheSecondImplementation : ThisOneIsAbstract
        {
            public override string WhoAreYou()
            {
                return "second";
            }

            public override int GetHashCode()
            {
                return "second".GetHashCode();
            }
        }
    }
}
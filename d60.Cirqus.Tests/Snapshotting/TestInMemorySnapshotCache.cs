using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using d60.Cirqus.Aggregates;
using d60.Cirqus.Snapshotting;
using NUnit.Framework;

namespace d60.Cirqus.Tests.Snapshotting
{
    [TestFixture]
    public class TestInMemorySnapshotCache : FixtureBase
    {
        [TestCase(typeof(SimpleRootWithOrdinaryField))]
        [TestCase(typeof(SimpleRootWithProperty))]
        [TestCase(typeof(SomeRootWithVariousDifficultThingsGoingOnForIt))]
        public void CanCloneDeepAndGoodWithMyRootsSerializationRoundtrip(Type rootType)
        {
            GetType()
                .GetMethod("RunSerializationRoundtripTestWith", BindingFlags.Instance | BindingFlags.NonPublic)
                .MakeGenericMethod(rootType)
                .Invoke(this, new object[0]);
        }

        [TestCase(typeof(SimpleRootWithOrdinaryField))]
        [TestCase(typeof(SimpleRootWithProperty))]
        [TestCase(typeof(SomeRootWithVariousDifficultThingsGoingOnForIt))]
        public void CanCloneDeepAndGoodWithMyRootsHashCodes(Type rootType)
        {
            GetType()
                .GetMethod("RunHashCodeTestWith", BindingFlags.Instance | BindingFlags.NonPublic)
                .MakeGenericMethod(rootType)
                .Invoke(this, new object[0]);
        }

        public class SimpleRootWithOrdinaryField : AggregateRoot
        {
            readonly string _thisIsAllIHave;

            public SimpleRootWithOrdinaryField()
            {
                _thisIsAllIHave = "hej";
            }

            public override int GetHashCode()
            {
                return _thisIsAllIHave.GetHashCode();
            }
        }

        public class SimpleRootWithProperty : AggregateRoot
        {
            public SimpleRootWithProperty()
            {
                ThisIsAllIHave = "hej";
            }

            public string ThisIsAllIHave { get; set; }

            public override int GetHashCode()
            {
                return ThisIsAllIHave.GetHashCode();
            }
        }

        // ReSharper disable UnusedMember.Local
        void RunSerializationRoundtripTestWith<TAggregateRoot>() where TAggregateRoot : AggregateRoot, new()
        {
            var id = Guid.NewGuid();
            var instance = new TAggregateRoot { Id = id };
            Console.WriteLine(instance.GetHashCode());

            var firstSerialization = InMemorySnapshotCache.CacheEntry.SerializeObject(instance);
            var roundtrippedSerialization = InMemorySnapshotCache.CacheEntry.SerializeObject(InMemorySnapshotCache.CacheEntry.DeserializeObject(firstSerialization));

            if (firstSerialization != roundtrippedSerialization)
            {
                throw new AssertionException(string.Format(@"Oh noes!!

{0}

{1}", firstSerialization, roundtrippedSerialization));
            }
        }
        // ReSharper restore UnusedMember.Local

        // ReSharper disable UnusedMember.Local
        void RunHashCodeTestWith<TAggregateRoot>() where TAggregateRoot : AggregateRoot, new()
        {
            var id = Guid.NewGuid();
            var instance = new TAggregateRoot { Id = id };

            var cache = new InMemorySnapshotCache();
            cache.PutCloneToCache(AggregateRootInfo<TAggregateRoot>.Old(instance, 0, 0));

            var rootInfo = cache.GetCloneFromCache<TAggregateRoot>(id, 0);
            var frozenInstance = rootInfo.AggregateRoot;

            cache.PutCloneToCache(AggregateRootInfo<TAggregateRoot>.Old(frozenInstance, 0, 0));

            Assert.That(frozenInstance.GetHashCode(), Is.EqualTo(instance.GetHashCode()));
        }
        // ReSharper restore UnusedMember.Local

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
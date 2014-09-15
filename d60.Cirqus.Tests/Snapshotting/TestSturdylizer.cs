using System;
using System.Reflection;
using d60.Cirqus.Aggregates;
using d60.Cirqus.Snapshotting;
using NUnit.Framework;

namespace d60.Cirqus.Tests.Snapshotting
{
    [TestFixture]
    public class TestSturdylizer : FixtureBase
    {
        Sturdylizer _sturdylizer;

        protected override void DoSetUp()
        {
            _sturdylizer = new Sturdylizer();
        }

        [TestCase(typeof(ChallengingSnapshotSpeciments.SimpleRootWithOrdinaryField))]
        [TestCase(typeof(ChallengingSnapshotSpeciments.SimpleRootWithProperty))]
        [TestCase(typeof(ChallengingSnapshotSpeciments.SimpleRootWithPublicField))]
        [TestCase(typeof(ChallengingSnapshotSpeciments.SomeRootWithVariousDifficultThingsGoingOnForIt))]
        public void CanCloneDeepAndGoodWithMyRootsSerializationRoundtrip(Type rootType)
        {
            GetType()
                .GetMethod("RunSerializationRoundtripTestWith", BindingFlags.Instance | BindingFlags.NonPublic)
                .MakeGenericMethod(rootType)
                .Invoke(this, new object[0]);
        }

        // ReSharper disable UnusedMember.Local
        void RunSerializationRoundtripTestWith<TAggregateRoot>() where TAggregateRoot : AggregateRoot, new()
        {
            var id = Guid.NewGuid();
            var instance = new TAggregateRoot { Id = id };
            Console.WriteLine(instance.GetHashCode());

            var firstSerialization = _sturdylizer.SerializeObject(instance);
            var roundtrippedSerialization = _sturdylizer.SerializeObject(_sturdylizer.DeserializeObject(firstSerialization));

            if (firstSerialization != roundtrippedSerialization)
            {
                throw new AssertionException(string.Format(@"Oh noes!!

{0}

{1}", firstSerialization, roundtrippedSerialization));
            }
        }
        // ReSharper restore UnusedMember.Local

    }
}
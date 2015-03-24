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

        [TestCase(typeof(ChallengingSnapshotSpecimens.SimpleRootWithOrdinaryField))]
        [TestCase(typeof(ChallengingSnapshotSpecimens.SimpleRootWithProperty))]
        [TestCase(typeof(ChallengingSnapshotSpecimens.SimpleRootWithPublicField))]
        [TestCase(typeof(ChallengingSnapshotSpecimens.SomeRootWithVariousDifficultThingsGoingOnForIt))]
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
            var instance = new TAggregateRoot { Id = "root_id" };
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

        [Test]
        public void WorksWithThisBadBoyToo()
        {
            var roow = new Root{ClassWithConstructionValidation = new ClassWithConstructionValidation(4)};

            var data = _sturdylizer.SerializeObject(roow);
            var roundtrippedRoow = (Root)_sturdylizer.DeserializeObject(data);

            Assert.That(roundtrippedRoow.ClassWithConstructionValidation.Value , Is.EqualTo(4));
        }

        public class Root : AggregateRoot
        {
            public ClassWithConstructionValidation ClassWithConstructionValidation { get; set; }
        }

        public class ClassWithConstructionValidation
        {
            public int Value { get; private set; }

            public ClassWithConstructionValidation(int value)
            {
                Value = value;
                if (value <= 0) throw new ArgumentException(string.Format("Oh noes: {0}", value));
            }
        }
    }
}
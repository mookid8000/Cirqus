using System;
using System.Reflection;
using d60.Cirqus.Aggregates;
using d60.Cirqus.Snapshotting;
using NUnit.Framework;

namespace d60.Cirqus.Tests.Snapshotting
{
    [TestFixture]
    public class TestInMemorySnapshotCache : FixtureBase
    {
        static readonly MethodInfo MethodInfo = typeof(TestInMemorySnapshotCache)
            .GetMethod("RunHashCodeTestWith", BindingFlags.Instance | BindingFlags.NonPublic);

        protected override void DoSetUp()
        {
            Assert.That(MethodInfo, Is.Not.Null, "Expected reflection to have found the test method RunHasCodeTestWith");
        }

        [TestCase(typeof(ChallengingSnapshotSpecimens.SimpleRootWithOrdinaryField))]
        [TestCase(typeof(ChallengingSnapshotSpecimens.SimpleRootWithProperty))]
        [TestCase(typeof(ChallengingSnapshotSpecimens.SimpleRootWithPublicField))]
        [TestCase(typeof(ChallengingSnapshotSpecimens.SomeRootWithVariousDifficultThingsGoingOnForIt))]
        public void CanCloneDeepAndGoodWithMyRootsHashCodes(Type rootType)
        {
            MethodInfo
                .MakeGenericMethod(rootType)
                .Invoke(this, new object[0]);
        }

        // ReSharper disable UnusedMember.Local
        void RunHashCodeTestWith<TAggregateRoot>() where TAggregateRoot : AggregateRoot, new()
        {
            var id = Guid.NewGuid();
            var instance = new TAggregateRoot {Id = id, GlobalSequenceNumberCutoff = 0};

            var cache = new InMemorySnapshotCache();
            cache.PutCloneToCache(AggregateRootInfo<TAggregateRoot>.Create(instance));

            var rootInfo = cache.GetCloneFromCache<TAggregateRoot>(id, 0);
            Assert.That(rootInfo, Is.Not.Null, "Expected to have found a root in the cache!");

            var frozenInstance = rootInfo.AggregateRoot;

            cache.PutCloneToCache(AggregateRootInfo<TAggregateRoot>.Create(frozenInstance));

            Assert.That(frozenInstance.GetHashCode(), Is.EqualTo(instance.GetHashCode()));
        }
        // ReSharper restore UnusedMember.Local
    }
}
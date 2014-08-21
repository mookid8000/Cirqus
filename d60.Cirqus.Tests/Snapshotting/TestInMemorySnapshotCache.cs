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
        [TestCase(typeof(ChallengingSnapshotSpeciments.SimpleRootWithOrdinaryField))]
        [TestCase(typeof(ChallengingSnapshotSpeciments.SimpleRootWithProperty))]
        [TestCase(typeof(ChallengingSnapshotSpeciments.SimpleRootWithPublicField))]
        [TestCase(typeof(ChallengingSnapshotSpeciments.SomeRootWithVariousDifficultThingsGoingOnForIt))]
        public void CanCloneDeepAndGoodWithMyRootsHashCodes(Type rootType)
        {
            GetType()
                .GetMethod("RunHashCodeTestWith", BindingFlags.Instance | BindingFlags.NonPublic)
                .MakeGenericMethod(rootType)
                .Invoke(this, new object[0]);
        }

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
    }
}
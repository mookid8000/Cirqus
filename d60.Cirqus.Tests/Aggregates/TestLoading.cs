using System;
using d60.Cirqus.Aggregates;
using d60.Cirqus.TestHelpers.Internals;
using NUnit.Framework;

namespace d60.Cirqus.Tests.Aggregates
{
    [TestFixture]
    public class TestLoading : FixtureBase
    {
        [Test]
        public void DefaultsToThrowingIfLoadedAggregateRootCannotBeFound()
        {
            var someRoot = new BeetRoot
            {
                AggregateRootRepository = new DefaultAggregateRootRepository(new InMemoryEventStore()),
                UnitOfWork = new InMemoryUnitOfWork()
            };

            Assert.Throws<ArgumentException>(someRoot.LoadOtherBeetRootWithDefaultBehavior);
        }

        [Test]
        public void CanBeToldToIgnoreNonExistenceOfOtherAggregateRoot()
        {
            var someRoot = new BeetRoot
            {
                AggregateRootRepository = new DefaultAggregateRootRepository(new InMemoryEventStore()),
                UnitOfWork = new InMemoryUnitOfWork()
            };

            Assert.DoesNotThrow(someRoot.LoadOtherBeetRootButOverrideBehavior);
        }


        class BeetRoot : AggregateRoot
        {
            public void LoadOtherBeetRootWithDefaultBehavior()
            {
                Load<BeetRoot>(Guid.NewGuid());
            }
            public void LoadOtherBeetRootButOverrideBehavior()
            {
                Load<BeetRoot>(Guid.NewGuid(), createIfNotExists: true);
            }
        }
    }
}
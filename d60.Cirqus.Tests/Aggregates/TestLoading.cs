using System;
using d60.Cirqus.Aggregates;
using d60.Cirqus.Config;
using d60.Cirqus.Serialization;
using d60.Cirqus.Testing.Internals;
using NUnit.Framework;

namespace d60.Cirqus.Tests.Aggregates
{
    [TestFixture]
    public class TestLoading : FixtureBase
    {
        static readonly JsonDomainEventSerializer DomainEventSerializer = new JsonDomainEventSerializer();
        static readonly DefaultDomainTypeNameMapper DefaultDomainTypeNameMapper = new DefaultDomainTypeNameMapper();

        [Test]
        public void DefaultsToThrowingIfLoadedAggregateRootCannotBeFound()
        {
            var someRoot = new BeetRoot
            {
                UnitOfWork = GetUnitOfWork()
            };

            Assert.Throws<ArgumentException>(someRoot.LoadOtherBeetRootWithDefaultBehavior);
        }

        [Test]
        public void CanBeToldToIgnoreNonExistenceOfOtherAggregateRoot()
        {
            var someRoot = new BeetRoot
            {
                UnitOfWork = GetUnitOfWork()
            };

            Assert.DoesNotThrow(someRoot.LoadOtherBeetRootButOverrideBehavior);
        }

        static InMemoryUnitOfWork GetUnitOfWork()
        {
            return new InMemoryUnitOfWork(new DefaultAggregateRootRepository(new InMemoryEventStore(DomainEventSerializer), DomainEventSerializer, DefaultDomainTypeNameMapper), DefaultDomainTypeNameMapper);
        }


        class BeetRoot : AggregateRoot
        {
            public void LoadOtherBeetRootWithDefaultBehavior()
            {
                Load<BeetRoot>("id1");
            }

            public void LoadOtherBeetRootButOverrideBehavior()
            {
                Load<BeetRoot>("id2", createIfNotExists: true);
            }
        }
    }
}
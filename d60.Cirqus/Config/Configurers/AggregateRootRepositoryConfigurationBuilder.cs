using System;
using d60.Cirqus.Aggregates;
using d60.Cirqus.Events;
using d60.Cirqus.Serialization;
using d60.Cirqus.Snapshotting;

namespace d60.Cirqus.Config.Configurers
{
    public class AggregateRootRepositoryConfigurationBuilder : ConfigurationBuilder<IAggregateRootRepository>
    {
        public AggregateRootRepositoryConfigurationBuilder(IRegistrar registrar) : base(registrar) { }

        /// <summary>
        /// Registers a <see cref="DefaultAggregateRootRepository"/> as the <see cref="IAggregateRootRepository"/> implementation. Since this is the
        /// default, there's no need to call this method explicitly.
        /// </summary>
        public void UseDefault()
        {
            Register(context =>
                new DefaultAggregateRootRepository(
                    context.Get<IEventStore>(),
                    context.Get<IDomainEventSerializer>(),
                    context.Get<IDomainTypeNameMapper>()));
        }

        /// <summary>
        /// Registers a <see cref="FactoryBasedAggregateRootRepository"/> as the <see cref="IAggregateRootRepository"/> implementation. 
        /// </summary>
        public void UseFactoryMethod(Func<Type, AggregateRoot> factoryMethod)
        {
            Register(context =>
                new FactoryBasedAggregateRootRepository(
                    context.Get<IEventStore>(),
                    context.Get<IDomainEventSerializer>(),
                    context.Get<IDomainTypeNameMapper>(),
                    factoryMethod));
        }

        /// <summary>
        /// Registers a <see cref="IAggregateRootRepository"/> as a decorator in front of the existing <see cref="InMemorySnapshotCache"/>
        /// which will use an <see cref="CachingAggregateRootRepositoryDecorator"/> to cache aggregate roots.
        /// </summary>
        public void EnableInMemorySnapshotCaching(int approximateMaxNumberOfCacheEntries)
        {
            Decorate(context =>
                new CachingAggregateRootRepositoryDecorator(
                    context.Get<IAggregateRootRepository>(),
                    new InMemorySnapshotCache
                    {
                        ApproximateMaxNumberOfCacheEntries = approximateMaxNumberOfCacheEntries
                    },
                    context.Get<IEventStore>(),
                    context.Get<IDomainEventSerializer>()));
        }
    }
}
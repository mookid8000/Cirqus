using System;
using d60.Cirqus.Aggregates;
using d60.Cirqus.Config.Configurers;
using d60.Cirqus.Events;
using d60.Cirqus.Serialization;

namespace d60.Cirqus.Snapshotting.New
{
    public static class SnapshottingConfigurationExtensions
    {
        public static void EnableSnapshotting(this OptionsConfigurationBuilder builder, Action<SnapshottingConfigurationBuilder> configureSnapshotting)
        {
            configureSnapshotting(new SnapshottingConfigurationBuilder(builder));

            builder.Decorate<IAggregateRootRepository>(c =>
            {
                var aggregateRootRepository = c.Get<IAggregateRootRepository>();
                var eventStore = c.Get<IEventStore>();
                var domainEventSerializer = c.Get<IDomainEventSerializer>();
                var snapshotStore = c.Get<ISnapshotStore>();

                return new NewSnapshottingAggregateRootRepositoryDecorator(aggregateRootRepository, eventStore, domainEventSerializer, snapshotStore);
            });
        }
    }

    public class SnapshottingConfigurationBuilder
    {
        readonly OptionsConfigurationBuilder _optionsConfigurationBuilder;

        public SnapshottingConfigurationBuilder(OptionsConfigurationBuilder optionsConfigurationBuilder)
        {
            _optionsConfigurationBuilder = optionsConfigurationBuilder;
        }

        public void Register(Func<ResolutionContext, ISnapshotStore> factory)
        {
            _optionsConfigurationBuilder.Register(factory);
        }

        public void Decorate(Func<ResolutionContext, ISnapshotStore> factory)
        {
            _optionsConfigurationBuilder.Decorate(factory);
        }
    }
}
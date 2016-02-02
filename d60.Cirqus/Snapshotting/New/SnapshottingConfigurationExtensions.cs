using System;
using d60.Cirqus.Aggregates;
using d60.Cirqus.Config.Configurers;
using d60.Cirqus.Events;
using d60.Cirqus.Serialization;

namespace d60.Cirqus.Snapshotting.New
{
    /// <summary>
    /// Configuration extensions for enabling aggregate root snapshots
    /// </summary>
    public static class SnapshottingConfigurationExtensions
    {
        /// <summary>
        /// Enables aggregate root snapshotting. When enabled, aggregate roots can be snapped by applying a <see cref="EnableSnapshotsAttribute"/> to them,
        /// using the <see cref="EnableSnapshotsAttribute.Version"/> property to leave old snapshots behind.
        /// </summary>
        public static void EnableSnapshotting(this OptionsConfigurationBuilder builder, Action<SnapshottingConfigurationBuilder> configureSnapshotting)
        {
            var snapshottingConfigurationBuilder = new SnapshottingConfigurationBuilder(builder);

            configureSnapshotting(snapshottingConfigurationBuilder);

            builder.Decorate<IAggregateRootRepository>(c =>
            {
                var aggregateRootRepository = c.Get<IAggregateRootRepository>();
                var eventStore = c.Get<IEventStore>();
                var domainEventSerializer = c.Get<IDomainEventSerializer>();
                var snapshotStore = c.Get<ISnapshotStore>();

                var threshold = snapshottingConfigurationBuilder.PreparationThreshold;

                return new NewSnapshottingAggregateRootRepositoryDecorator(aggregateRootRepository, eventStore, domainEventSerializer, snapshotStore, threshold);
            });
        }
    }
}
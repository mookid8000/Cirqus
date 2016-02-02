using System;
using d60.Cirqus.Config.Configurers;

namespace d60.Cirqus.Snapshotting.New
{
    /// <summary>
    /// Configuration builder for specifying options for snapshotting during a call to <see cref="SnapshottingConfigurationExtensions.EnableSnapshotting"/>
    /// </summary>
    public class SnapshottingConfigurationBuilder
    {
        readonly OptionsConfigurationBuilder _optionsConfigurationBuilder;

        internal SnapshottingConfigurationBuilder(OptionsConfigurationBuilder optionsConfigurationBuilder)
        {
            _optionsConfigurationBuilder = optionsConfigurationBuilder;

            PreparationThreshold = TimeSpan.FromSeconds(0.5);
        }

        internal TimeSpan PreparationThreshold
        {
            get; set;
        }

        /// <summary>
        /// Sets the threshold for the time elapsed during preparation after which a new snapshot will be immediately created
        /// </summary>
        public SnapshottingConfigurationBuilder SetPreparationThreshold(TimeSpan threshold)
        {
            PreparationThreshold = threshold;
            return this;
        }

        /// <summary>
        /// Registers the given factory method as a primary resolver of a <see cref="ISnapshotStore"/>
        /// </summary>
        public void Register(Func<ResolutionContext, ISnapshotStore> factory)
        {
            _optionsConfigurationBuilder.Register(factory);
        }

        /// <summary>
        /// Registers the given factory method as a decorator resolver of a <see cref="ISnapshotStore"/>
        /// </summary>
        public void Decorate(Func<ResolutionContext, ISnapshotStore> factory)
        {
            _optionsConfigurationBuilder.Decorate(factory);
        }
    }
}
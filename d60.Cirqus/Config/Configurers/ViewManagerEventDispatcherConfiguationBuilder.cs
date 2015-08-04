using d60.Cirqus.Views;
using d60.Cirqus.Views.ViewManagers;

namespace d60.Cirqus.Config.Configurers
{
    /// <summary>
    /// Configuration builder that allows for configuring a <see cref="ViewManagerEventDispatcher"/>
    /// </summary>
    public class ViewManagerEventDispatcherConfiguationBuilder : ConfigurationBuilder<ViewManagerEventDispatcher>
    {
        /// <summary>
        /// Creates the builder
        /// </summary>
        public ViewManagerEventDispatcherConfiguationBuilder(IRegistrar registrar) : base(registrar) {}

        /// <summary>
        /// Uses the given wait handle in the view dispatcher, allowing you to wait for specific views (or all views) to catch up to a certain state
        /// </summary>
        public ViewManagerEventDispatcherConfiguationBuilder WithWaitHandle(ViewManagerWaitHandle handle)
        {
            RegisterInstance(handle);
            return this;
        }

        /// <summary>
        /// Configures the event dispatcher to persist its state after <paramref name="max"/> events at most
        /// </summary>
        public ViewManagerEventDispatcherConfiguationBuilder WithMaxDomainEventsPerBatch(int max)
        {
            RegisterInstance(max);
            return this;
        }

        /// <summary>
        /// Registers the given profiler with the event dispatcher, allowing you to aggregate timing information from the view subsystem
        /// </summary>
        public ViewManagerEventDispatcherConfiguationBuilder WithProfiler(IViewManagerProfiler profiler)
        {
            RegisterInstance(profiler);
            return this;
        }

        /// <summary>
        /// Enables the automatic view manager distribution service which periodically ensures that views are relatively fairly distributed
        /// among the available processes.
        /// </summary>
        /// <param name="id"></param>
        public ViewManagerEventDispatcherConfiguationBuilder AutomaticallyRedistributeViews(string id, IAutoDistributionState autoDistributionState)
        {
            RegisterInstance(new AutoDistributionViewManagerEventDispatcher(id, autoDistributionState));
            return this;
        }
    }
}
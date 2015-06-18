using System;

namespace d60.Cirqus.Config.Configurers
{
    /// <summary>
    /// Configuration builder that allows for configuring other things
    /// </summary>
    public interface IOptionalConfiguration<T>
    {
        /// <summary>
        /// Begins the aggregate root repository configuration, which can be completed by supplying an action that makes an additional call to the passed-in <see cref="AggregateRootRepositoryConfigurationBuilder"/>
        /// </summary>
        IOptionalConfiguration<T> AggregateRootRepository(Action<AggregateRootRepositoryConfigurationBuilder> configure);

        /// <summary>
        /// Begins the event dispatcher configuration, which can be completed by supplying an action that makes an additional call to the passed-in <see cref="EventDispatcherConfigurationBuilder"/>. You
        /// will often call this one with <see cref="EventDispatcherConfigurationBuilder.UseViewManagerEventDispatcher"/> to use the built-in view manager subsystem to project events into materialized views
        /// </summary>
        IOptionalConfiguration<T> EventDispatcher(Action<EventDispatcherConfigurationBuilder> configure);

        /// <summary>
        /// Begins the additional options configuration, which can be completed by supplying an action that makes an additional call to the passed-in <see cref="OptionsConfigurationBuilder"/>
        /// </summary>
        IOptionalConfiguration<T> Options(Action<OptionsConfigurationBuilder> func);

        /// <summary>
        /// Finishes off the configuration (returns an implementation of <see cref="ICommandProcessor"/> if you're configuring Cirqus 4real, or the TestContext if you're going to do some testing)
        /// </summary>
        T Create();
    }
}
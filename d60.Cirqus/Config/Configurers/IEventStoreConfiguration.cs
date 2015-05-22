using System;

namespace d60.Cirqus.Config.Configurers
{
    /// <summary>
    /// Configuration builder thingie that allows for configuring which event store to use
    /// </summary>
    public interface IEventStoreConfiguration
    {
        /// <summary>
        /// Begins the event store configuration, which can be completed by supplying an action that makes an additional call to the passed-in <see cref="EventStoreConfigurationBuilder"/>
        /// </summary>
        IOptionalConfiguration<ICommandProcessor> EventStore(Action<EventStoreConfigurationBuilder> configure);
    }
}
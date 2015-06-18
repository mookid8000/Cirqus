using System;

namespace d60.Cirqus.Config.Configurers
{
    /// <summary>
    /// Combined configuration builder that allows for configuring either logging and then event store, or just go right on to the event store
    /// </summary>
    public interface ILoggingAndEventStoreConfiguration : IEventStoreConfiguration
    {
        /// <summary>
        /// Begins the logging configuration, which can be completed by supplying an action that makes an additional call to the passed-in <see cref="LoggingConfigurationBuilder"/>
        /// </summary>
        IEventStoreConfiguration Logging(Action<LoggingConfigurationBuilder> configure);
    }
}
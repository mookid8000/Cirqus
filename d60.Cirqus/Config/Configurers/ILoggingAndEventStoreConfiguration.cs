using System;

namespace d60.Cirqus.Config.Configurers
{
    public interface ILoggingAndEventStoreConfiguration : IEventStoreConfiguration
    {
        IEventStoreConfiguration Logging(Action<LoggingConfigurationBuilder> configure);
    }
}
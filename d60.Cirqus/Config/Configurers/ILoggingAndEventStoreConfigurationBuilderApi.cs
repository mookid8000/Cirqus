using System;

namespace d60.Cirqus.Config.Configurers
{
    public interface ILoggingAndEventStoreConfigurationBuilderApi : IEventStoreConfigurationBuilderApi
    {
        IEventStoreConfigurationBuilderApi Logging(Action<LoggingConfigurationBuilder> configure);
    }
}
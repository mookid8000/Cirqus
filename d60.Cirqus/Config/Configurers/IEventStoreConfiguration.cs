using System;

namespace d60.Cirqus.Config.Configurers
{
    public interface IEventStoreConfiguration
    {
        IOptionalConfiguration EventStore(Action<EventStoreConfigurationBuilder> configure);
    }
}
using System;

namespace d60.Cirqus.Config.Configurers
{
    public interface IEventStoreConfiguration
    {
        IOptionalConfiguration<ICommandProcessor> EventStore(Action<EventStoreConfigurationBuilder> configure);
    }
}
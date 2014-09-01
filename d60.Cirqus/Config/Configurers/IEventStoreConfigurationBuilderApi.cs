using System;

namespace d60.Cirqus.Config.Configurers
{
    public interface IEventStoreConfigurationBuilderApi
    {
        IAggregateRootRepositoryConfigurationBuilderApi EventStore(Action<EventStoreConfigurationBuilder> configure);
    }
}
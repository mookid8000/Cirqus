using System;

namespace d60.Cirqus.Config.Configurers
{
    public interface IEventDispatcherConfigurationBuilderApi
    {
        IFullConfiguration EventDispatcher(Action<EventDispatcherConfigurationBuilder> configure);
    }
}
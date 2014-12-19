using System;

namespace d60.Cirqus.Config.Configurers
{
    public interface IOptionalConfiguration
    {
        IOptionalConfiguration AggregateRootRepository(Action<AggregateRootRepositoryConfigurationBuilder> configure);
        IOptionalConfiguration EventDispatcher(Action<EventDispatcherConfigurationBuilder> configure); 
        IOptionalConfiguration Options(Action<OptionsConfigurationBuilder> func);
        ICommandProcessor Create();
    }
}
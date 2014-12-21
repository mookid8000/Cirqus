using System;

namespace d60.Cirqus.Config.Configurers
{
    public interface IOptionalConfiguration<T>
    {
        IOptionalConfiguration<T> AggregateRootRepository(Action<AggregateRootRepositoryConfigurationBuilder> configure);
        IOptionalConfiguration<T> EventDispatcher(Action<EventDispatcherConfigurationBuilder> configure); 
        IOptionalConfiguration<T> Options(Action<OptionsConfigurationBuilder> func);
        T Create();
    }
}
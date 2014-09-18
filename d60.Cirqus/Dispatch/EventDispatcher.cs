using System;
using d60.Cirqus.Config.Configurers;

namespace d60.Cirqus.Dispatch
{
    public class EventDispatcher
    {
        public static StandAloneEventDispatcherConfigurationBuilder With()
        {
            return new StandAloneEventDispatcherConfigurationBuilder();
        } 
    }

    public class StandAloneEventDispatcherConfigurationBuilder
    {
        readonly ConfigurationContainer _container = new ConfigurationContainer();
        
        public StandAloneEventDispatcherConfigurationBuilder()
        {
            throw new NotImplementedException();
        }
    }
}
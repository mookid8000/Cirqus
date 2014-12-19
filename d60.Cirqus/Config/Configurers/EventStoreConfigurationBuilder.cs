using d60.Cirqus.Events;

namespace d60.Cirqus.Config.Configurers
{
    public class EventStoreConfigurationBuilder : ConfigurationBuilder<IEventStore>
    {
        public EventStoreConfigurationBuilder(IRegistrar registrar) : base(registrar) { }
    }
}
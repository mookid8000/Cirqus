namespace d60.Cirqus.Config.Configurers
{
    public class EventStoreConfigurationBuilder
    {
        readonly IServiceRegistrar _serviceRegistrar;

        public EventStoreConfigurationBuilder(IServiceRegistrar serviceRegistrar)
        {
            _serviceRegistrar = serviceRegistrar;
        }

        public IServiceRegistrar ServiceRegistrar
        {
            get { return _serviceRegistrar; }
        }
    }
}
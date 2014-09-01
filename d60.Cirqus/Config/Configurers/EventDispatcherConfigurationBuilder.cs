namespace d60.Cirqus.Config.Configurers
{
    public class EventDispatcherConfigurationBuilder
    {
        readonly IServiceRegistrar _serviceRegistrar;

        public EventDispatcherConfigurationBuilder(IServiceRegistrar serviceRegistrar)
        {
            _serviceRegistrar = serviceRegistrar;
        }

        public IServiceRegistrar ServiceRegistrar
        {
            get { return _serviceRegistrar; }
        }
    }
}
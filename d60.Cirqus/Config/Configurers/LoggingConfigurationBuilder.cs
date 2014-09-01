namespace d60.Cirqus.Config.Configurers
{
    public class LoggingConfigurationBuilder
    {
        readonly IServiceRegistrar _serviceRegistrar;

        public LoggingConfigurationBuilder(IServiceRegistrar serviceRegistrar)
        {
            _serviceRegistrar = serviceRegistrar;
        }

        public IServiceRegistrar ServiceRegistrar
        {
            get { return _serviceRegistrar; }
        }
    }
}
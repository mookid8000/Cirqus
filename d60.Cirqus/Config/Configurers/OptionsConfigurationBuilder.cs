namespace d60.Cirqus.Config.Configurers
{
    public class OptionsConfigurationBuilder
    {
        readonly IServiceRegistrar _serviceRegistrar;

        public IServiceRegistrar ServiceRegistrar
        {
            get { return _serviceRegistrar; }
        }

        public OptionsConfigurationBuilder(IServiceRegistrar serviceRegistrar)
        {
            _serviceRegistrar = serviceRegistrar;
        }
    }
}
namespace d60.Cirqus.Config.Configurers
{
    public class AggregateRootRepositoryConfigurationBuilder
    {
        readonly IServiceRegistrar _serviceRegistrar;

        public AggregateRootRepositoryConfigurationBuilder(IServiceRegistrar serviceRegistrar)
        {
            _serviceRegistrar = serviceRegistrar;
        }

        public IServiceRegistrar ServiceRegistrar
        {
            get { return _serviceRegistrar; }
        }
    }
}
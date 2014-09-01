namespace d60.Cirqus.Config.Configurers
{
    public class AggregateRootRepositoryConfigurationBuilder
    {
        readonly IRegistrar _registrar;

        public AggregateRootRepositoryConfigurationBuilder(IRegistrar registrar)
        {
            _registrar = registrar;
        }

        public IRegistrar Registrar
        {
            get { return _registrar; }
        }
    }
}
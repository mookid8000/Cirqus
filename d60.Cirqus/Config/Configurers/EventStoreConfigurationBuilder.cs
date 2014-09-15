namespace d60.Cirqus.Config.Configurers
{
    public class EventStoreConfigurationBuilder
    {
        readonly IRegistrar _registrar;

        public EventStoreConfigurationBuilder(IRegistrar registrar)
        {
            _registrar = registrar;
        }

        public IRegistrar Registrar
        {
            get { return _registrar; }
        }
    }
}
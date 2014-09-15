namespace d60.Cirqus.Config.Configurers
{
    public class EventDispatcherConfigurationBuilder
    {
        readonly IRegistrar _registrar;

        public EventDispatcherConfigurationBuilder(IRegistrar registrar)
        {
            _registrar = registrar;
        }

        public IRegistrar Registrar
        {
            get { return _registrar; }
        }
    }
}
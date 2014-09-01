namespace d60.Cirqus.Config.Configurers
{
    public class LoggingConfigurationBuilder
    {
        readonly IRegistrar _registrar;

        public LoggingConfigurationBuilder(IRegistrar registrar)
        {
            _registrar = registrar;
        }

        public IRegistrar Registrar
        {
            get { return _registrar; }
        }
    }
}
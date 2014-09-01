namespace d60.Cirqus.Config.Configurers
{
    public class OptionsConfigurationBuilder
    {
        readonly IRegistrar _registrar;

        public IRegistrar Registrar
        {
            get { return _registrar; }
        }

        public OptionsConfigurationBuilder(IRegistrar registrar)
        {
            _registrar = registrar;
        }
    }
}
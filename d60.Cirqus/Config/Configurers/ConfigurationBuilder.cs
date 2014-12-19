using System;

namespace d60.Cirqus.Config.Configurers
{
    public abstract class ConfigurationBuilder
    {
        readonly IRegistrar _registrar;

        protected ConfigurationBuilder(IRegistrar registrar)
        {
            _registrar = registrar;
        }

        public IRegistrar Registrar
        {
            get { return _registrar; }
        }

        /// <summary>
        /// Registers a factory method for the given service
        /// </summary>
        public void Register<TService>(Func<ResolutionContext, TService> serviceFactory, bool decorator = false, bool multi = false)
        {
            _registrar.Register(serviceFactory, decorator, multi);
        }

        /// <summary>
        /// Registers a specific instance (which by definition is not a decorator)
        /// </summary>
        public void Use<TService>(TService instance, bool multi = false)
        {
            _registrar.RegisterInstance(instance, multi);
        }
    }
}
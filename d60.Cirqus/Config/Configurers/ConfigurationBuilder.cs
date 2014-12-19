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
        /// Registers a factory method for decorating the given type
        /// </summary>
        public void Decorate<TService>(Func<TService, TService> serviceFactory)
        {
            Registrar.Register(context => serviceFactory(context.Get<TService>()), decorator: true, multi: false);
        }

        /// <summary>
        /// Registers a specific instance
        /// </summary>
        public void Use<TService>(TService instance, bool multi = false)
        {
            Registrar.RegisterInstance(instance, multi);
        }
    }

    public abstract class ConfigurationBuilder<TService> : ConfigurationBuilder
    {
        protected ConfigurationBuilder(IRegistrar registrar) : base(registrar) {}

        /// <summary>
        /// Registers a factory method for decorating the given type
        /// </summary>
        public void Decorate(Func<TService, TService> serviceFactory)
        {
            Decorate<TService>(serviceFactory);
        }

        /// <summary>
        /// Registers a factory method for decorating the given type
        /// </summary>
        public void Use(TService instance, bool multi = false)
        {
            Use<TService>(instance, multi);
        }
    }
}
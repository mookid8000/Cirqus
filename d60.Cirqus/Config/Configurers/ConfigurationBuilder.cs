using System;

namespace d60.Cirqus.Config.Configurers
{
    /// <summary>
    /// Configuration builder that is used to register factory methods for various services
    /// </summary>
    public abstract class ConfigurationBuilder
    {
        protected IRegistrar Registrar;

        /// <summary>
        /// Constructs the builder
        /// </summary>
        protected ConfigurationBuilder(IRegistrar registrar)
        {
            Registrar = registrar;
        }

        /// <summary>
        /// Registers a factory method for <typeparamref name="TService"/>
        /// </summary>
        public void Register<TService>(Func<ResolutionContext, TService> serviceFactory)
        {
            Registrar.Register(serviceFactory);
        }

        /// <summary>
        /// Registers a specific instance (which by definition is not a decorator) for <typeparamref name="TService"/>
        /// </summary>
        public void RegisterInstance<TService>(TService instance, bool multi = false)
        {
            Registrar.RegisterInstance(instance, multi);
        }

        /// <summary>
        /// Registers a factory method for decorating <typeparamref name="TService"/>
        /// </summary>
        public void Decorate<TService>(Func<ResolutionContext, TService> serviceFactory)
        {
            Registrar.Decorate(serviceFactory);
        }

        /// <summary>
        /// Checks whether the given service type has a registration. Optionally checks whether a primary (i.e. non-decorator) is present.
        /// </summary>
        public bool HasService<TService>(bool checkForPrimary = false)
        {
            return Registrar.HasService<TService>(checkForPrimary);
        }
    }

    /// <summary>
    /// Typed configuration builder that can be used to fixate the type that the registered factory must return
    /// </summary>
    public abstract class ConfigurationBuilder<TService> : ConfigurationBuilder
    {
        /// <summary>
        /// Constructs the builder
        /// </summary>
        protected ConfigurationBuilder(IRegistrar registrar) : base(registrar) {}

        /// <summary>
        /// Registers a factory method for <typeparamref name="TService"/>
        /// </summary>
        public void Register(Func<ResolutionContext, TService> serviceFactory)
        {
            Registrar.Register(serviceFactory);
        }

        /// <summary>
        /// Registers a specific instance (which by definition is not a decorator) for <typeparamref name="TService"/>
        /// </summary>
        public void RegisterInstance(TService instance, bool multi = false)
        {
            Registrar.RegisterInstance(instance, multi);
        }

        /// <summary>
        /// Registers a factory method for decorating <typeparamref name="TService"/>
        /// </summary>
        public void Decorate(Func<ResolutionContext, TService> serviceFactory)
        {
            Registrar.Decorate(serviceFactory);
        }
    }
}
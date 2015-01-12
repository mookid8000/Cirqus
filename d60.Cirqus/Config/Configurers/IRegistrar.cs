using System;

namespace d60.Cirqus.Config.Configurers
{
    public interface IRegistrar
    {
        /// <summary>
        /// Registers a factory method for the given service
        /// </summary>
        void Register<TService>(Func<ResolutionContext, TService> serviceFactory);

        /// <summary>
        /// Registers a specific instance (which by definition is not a decorator)
        /// </summary>
        void RegisterInstance<TService>(TService instance, bool multi = false);

        /// <summary>
        /// Registers a factory method for decorating the given type
        /// </summary>
        void Decorate<TService>(Func<ResolutionContext, TService> serviceFactory);

        /// <summary>
        /// Checks whether the given service type has a registration. Optionally checks whether a primary (i.e. non-decorator) is present.
        /// </summary>
        bool HasService<TService>(bool checkForPrimary = false);
    }
}
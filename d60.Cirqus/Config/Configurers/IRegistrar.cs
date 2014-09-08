using System;

namespace d60.Cirqus.Config.Configurers
{
    public interface IRegistrar
    {
        /// <summary>
        /// Registers a factory method for the given service
        /// </summary>
        void Register<TService>(Func<ResolutionContext, TService> serviceFactory, bool decorator = false);
        void RegisterOptionConfig(Action<Options> optionAction);
        bool HasService<TService>();
    }
}
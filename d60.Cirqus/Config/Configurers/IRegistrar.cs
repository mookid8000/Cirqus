using System;

namespace d60.Cirqus.Config.Configurers
{
    public interface IRegistrar
    {
        void Register<TService>(Func<ResolutionContext, TService> serviceFactory, bool decorator = false);
        void RegisterOptionConfig(Action<Options> optionAction);
        TService Get<TService>(ResolutionContext context);
    }
}
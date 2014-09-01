using System;

namespace d60.Cirqus.Config.Configurers
{
    public interface IServiceRegistrar
    {
        void Register<TService>(Func<TService> serviceFactory);
        void RegisterOptionConfig(Action<Options> optionAction);
        TService Get<TService>();
    }
}
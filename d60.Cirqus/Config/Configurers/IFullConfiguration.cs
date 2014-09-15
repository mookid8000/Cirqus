using System;

namespace d60.Cirqus.Config.Configurers
{
    public interface IFullConfiguration
    {
        IFullConfiguration Options(Action<OptionsConfigurationBuilder> func);
        ICommandProcessor Create();
    }
}
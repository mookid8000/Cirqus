using System;

namespace d60.Cirqus.Config.Configurers
{
    public interface IFullConfigurationConfigurationBuilderApi
    {
        IFullConfiguration Options(Action<OptionsConfigurationBuilder> configure);
    }
}
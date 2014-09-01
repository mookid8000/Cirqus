using d60.Cirqus.Config.Configurers;
using d60.Cirqus.Logging;

namespace d60.Cirqus.NLog.Config
{
    public static class NLogConfigurationExtensions
    {
        public static void UseNLog(this LoggingConfigurationBuilder builder)
        {
            CirqusLoggerFactory.Current = new NLogLoggerFactory();
        }
    }
}
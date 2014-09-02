using d60.Cirqus.Config.Configurers;
using d60.Cirqus.Logging;

namespace d60.Cirqus.Serilog.Config
{
    public static class SerilogConfigurationExtensions
    {
        public static void UseSerilog(this LoggingConfigurationBuilder builder)
        {
            CirqusLoggerFactory.Current = new SerilogLoggerFactory();
        }
    }
}
using d60.Cirqus.Config.Configurers;
using d60.Cirqus.Logging;
using Serilog;

namespace d60.Cirqus.Serilog.Config
{
    public static class SerilogConfigurationExtensions
    {
        public static void UseSerilog(this LoggingConfigurationBuilder builder, LoggerConfiguration configuration)
        {
            CirqusLoggerFactory.Current = new SerilogLoggerFactory(configuration);
        }
    }
}
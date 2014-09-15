using d60.Cirqus.Config.Configurers;
using d60.Cirqus.Logging;
using Serilog;

namespace d60.Cirqus.Serilog.Config
{
    public static class SerilogConfigurationExtensions
    {
        /// <summary>
        /// Uses Serilog, pulling typed loggers from the specified <see cref="LoggerConfiguration"/>
        /// </summary>
        public static void UseSerilog(this LoggingConfigurationBuilder builder, LoggerConfiguration configuration)
        {
            CirqusLoggerFactory.Current = new SerilogLoggerFactory(configuration);
        }
        
        /// <summary>
        /// Uses Serilog, pulling typed logger from the given <seealso cref="baseLogger"/>
        /// </summary>
        public static void UseSerilog(this LoggingConfigurationBuilder builder, ILogger baseLogger)
        {
            CirqusLoggerFactory.Current = new SerilogLoggerFactory(baseLogger);
        }
        
        /// <summary>
        /// Uses Serilog and uses Serilog's static logger factory to create typed loggers
        /// </summary>
        public static void UseSerilog(this LoggingConfigurationBuilder builder)
        {
            CirqusLoggerFactory.Current = new SerilogLoggerFactory();
        }
    }
}
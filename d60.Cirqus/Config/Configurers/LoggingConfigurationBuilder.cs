using d60.Cirqus.Logging;
using d60.Cirqus.Logging.Console;
using d60.Cirqus.Logging.Null;

namespace d60.Cirqus.Config.Configurers
{
    public class LoggingConfigurationBuilder : ConfigurationBuilder
    {
        public LoggingConfigurationBuilder(IRegistrar registrar) : base(registrar) { }

        /// <summary>
        /// Configures Cirqus to log using the console.
        /// </summary>
        public void UseConsole(Logger.Level minLevel = Logger.Level.Info)
        {
            Use(new ConsoleLoggerFactory(minLevel: minLevel));
        }

        /// <summary>
        /// Configures Cirqus to not log anything at all.
        /// </summary>
        public void None()
        {
            Use(new NullLoggerFactory());
        }

        /// <summary>
        /// Configures Cirqus get its logger using specified factory.
        /// </summary>
        public void Use(CirqusLoggerFactory factory)
        {
            CirqusLoggerFactory.Current = factory;
        }
    }
}
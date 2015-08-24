using System.Diagnostics;
using d60.Cirqus.Logging;
using d60.Cirqus.Logging.Console;
using d60.Cirqus.Logging.Debug;
using d60.Cirqus.Logging.Null;

namespace d60.Cirqus.Config.Configurers
{
    /// <summary>
    /// Configuration builder for configuring logging
    /// </summary>
    public class LoggingConfigurationBuilder : ConfigurationBuilder
    {
        /// <summary>
        /// Constructs the builder
        /// </summary>
        public LoggingConfigurationBuilder(IRegistrar registrar) : base(registrar) { }

        /// <summary>
        /// Configures Cirqus to log using the console.
        /// </summary>
        public void UseConsole(Logger.Level minLevel = Logger.Level.Info)
        {
            Use(new ConsoleLoggerFactory(minLevel: minLevel));
        }

        /// <summary>
        /// Configures Cirqus to log using <see cref="Debug.WriteLine(string)"/>.
        /// </summary>
        public void UseDebug(Logger.Level minLevel = Logger.Level.Info)
        {
            Use(new DebugLoggerFactory(minLevel: minLevel));
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
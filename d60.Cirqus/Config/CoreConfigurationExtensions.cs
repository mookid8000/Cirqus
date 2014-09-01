using System;
using d60.Cirqus.Aggregates;
using d60.Cirqus.Config.Configurers;
using d60.Cirqus.Events;
using d60.Cirqus.Logging;
using d60.Cirqus.Logging.Console;
using d60.Cirqus.Logging.Null;
using d60.Cirqus.Views.ViewManagers;

namespace d60.Cirqus.Config
{
    public static class CoreConfigurationExtensions
    {
        public static void UseDefaultAggregateRootRepository(this AggregateRootRepositoryConfigurationBuilder builder)
        {
            builder.Registrar
                .Register<IAggregateRootRepository>(() => new DefaultAggregateRootRepository(builder.Registrar.Get<IEventStore>()));
        }

        public static void ViewManagerEventDispatcher(this EventDispatcherConfigurationBuilder builder, params IViewManager[] viewManagers)
        {
            builder.Registrar
                .Register<IEventDispatcher>(() => new ViewManagerEventDispatcher(builder.Registrar.Get<IAggregateRootRepository>(), viewManagers));
        }

        public static void PurgeViewsAtStartup(this OptionsConfigurationBuilder builder, bool purgeViewsAtStartup = false)
        {
            builder.Registrar.RegisterOptionConfig(o => o.PurgeExistingViews = purgeViewsAtStartup);
        }

        public static void AddDomainExceptionType<TException>(this OptionsConfigurationBuilder builder) where TException : Exception
        {
            builder.Registrar.RegisterOptionConfig(o => o.AddDomainExceptionType<TException>());
        }

        public static void UseConsole(this LoggingConfigurationBuilder builder, Logger.Level minLevel = Logger.Level.Info)
        {
            CirqusLoggerFactory.Current = new ConsoleLoggerFactory(minLevel: minLevel);
        }

        public static void None(this LoggingConfigurationBuilder builder)
        {
            CirqusLoggerFactory.Current = new NullLoggerFactory();
        }
    }
}
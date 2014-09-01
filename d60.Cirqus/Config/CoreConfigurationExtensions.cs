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
            builder.ServiceRegistrar
                .Register<IAggregateRootRepository>(() => new DefaultAggregateRootRepository(builder.ServiceRegistrar.Get<IEventStore>()));
        }

        public static void ViewManagerEventDispatcher(this EventDispatcherConfigurationBuilder builder, params IViewManager[] viewManagers)
        {
            builder.ServiceRegistrar
                .Register<IEventDispatcher>(() => new ViewManagerEventDispatcher(builder.ServiceRegistrar.Get<IAggregateRootRepository>(), viewManagers));
        }

        public static void PurgeViewsAtStartup(this OptionsConfigurationBuilder builder, bool purgeViewsAtStartup = false)
        {
            builder.ServiceRegistrar.RegisterOptionConfig(o => o.PurgeExistingViews = purgeViewsAtStartup);
        }

        public static void AddDomainExceptionType<TException>(this OptionsConfigurationBuilder builder) where TException : Exception
        {
            builder.ServiceRegistrar.RegisterOptionConfig(o => o.AddDomainExceptionType<TException>());
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
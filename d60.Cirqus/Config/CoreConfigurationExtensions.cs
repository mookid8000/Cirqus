using System;
using d60.Cirqus.Aggregates;
using d60.Cirqus.Config.Configurers;
using d60.Cirqus.Events;
using d60.Cirqus.Logging;
using d60.Cirqus.Logging.Console;
using d60.Cirqus.Logging.Null;
using d60.Cirqus.Snapshotting;
using d60.Cirqus.Views.ViewManagers;

namespace d60.Cirqus.Config
{
    public static class CoreConfigurationExtensions
    {
        public static void EnableInMemorySnapshotCaching(this AggregateRootRepositoryConfigurationBuilder builder, int approximateMaxNumberOfCacheEntries)
        {
            builder.Registrar
                .Register<IAggregateRootRepository>(
                    context => new CachingAggregateRootRepositoryDecorator(
                        context.Get<IAggregateRootRepository>(),
                        new InMemorySnapshotCache
                        {
                            ApproximateMaxNumberOfCacheEntries = approximateMaxNumberOfCacheEntries
                        },
                        context.Get<IEventStore>()),
                    decorator: true
                );

        }

        public static void UseDefault(this AggregateRootRepositoryConfigurationBuilder builder)
        {
            builder.Registrar
                .Register<IAggregateRootRepository>(context => new DefaultAggregateRootRepository(context.Get<IEventStore>()));
        }

        public static void UseViewManagerEventDispatcher(this EventDispatcherConfigurationBuilder builder, params IViewManager[] viewManagers)
        {
            builder.Registrar
                .Register<IEventDispatcher>(context => new ViewManagerEventDispatcher(context.Get<IAggregateRootRepository>(), viewManagers));
        }

        public static void PurgeExistingViews(this OptionsConfigurationBuilder builder, bool purgeViewsAtStartup = false)
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
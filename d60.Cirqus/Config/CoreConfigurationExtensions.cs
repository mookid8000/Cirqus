using System;
using d60.Cirqus.Aggregates;
using d60.Cirqus.Config.Configurers;
using d60.Cirqus.Events;
using d60.Cirqus.Logging;
using d60.Cirqus.Logging.Console;
using d60.Cirqus.Logging.Null;
using d60.Cirqus.Snapshotting;
using d60.Cirqus.Views;
using d60.Cirqus.Views.ViewManagers.New;
using d60.Cirqus.Views.ViewManagers.Old;

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
            RegisterEventDispatcher(builder, context => new ViewManagerEventDispatcher(context.Get<IAggregateRootRepository>(), viewManagers));
        }

        public static void UseNewViewManagerEventDispatcher(this EventDispatcherConfigurationBuilder builder, params IManagedView[] managedViews)
        {
            RegisterEventDispatcher(builder, context => new NewViewManagerEventDispatcher(
                context.Get<IAggregateRootRepository>(),
                context.Get<IEventStore>(), managedViews));
        }

        public static void UseNewViewManagerEventDispatcher(this EventDispatcherConfigurationBuilder builder, ViewManagerWaitHandle waitHandle, params IManagedView[] managedViews)
        {
            RegisterEventDispatcher(builder, context =>
            {
                var eventDispatcher = new NewViewManagerEventDispatcher(
                    context.Get<IAggregateRootRepository>(),
                    context.Get<IEventStore>(), managedViews);

                waitHandle.Register(eventDispatcher);

                return eventDispatcher;
            });
        }

        public static void UseEventDispatcher(this EventDispatcherConfigurationBuilder builder, IEventDispatcher eventDispatcher)
        {
            RegisterEventDispatcher(builder, context => eventDispatcher);
        }

        static void RegisterEventDispatcher(EventDispatcherConfigurationBuilder builder, Func<ResolutionContext, IEventDispatcher> eventDispatcherFunc)
        {
            if (builder.Registrar.HasService<IEventDispatcher>())
            {
                builder.Registrar
                    .Register<IEventDispatcher>(context => new CompositeEventDispatcher(context.Get<IEventDispatcher>(), eventDispatcherFunc(context)), decorator: true);
            }
            else
            {
                builder.Registrar.Register(eventDispatcherFunc);
            }
        }

        public static void PurgeExistingViews(this OptionsConfigurationBuilder builder, bool purgeViewsAtStartup = false)
        {
            builder.Registrar.RegisterOptionConfig(o => o.PurgeExistingViews = purgeViewsAtStartup);
        }

        public static void AddDomainExceptionType<TException>(this OptionsConfigurationBuilder builder) where TException : Exception
        {
            builder.Registrar.RegisterOptionConfig(o => o.AddDomainExceptionType<TException>());
        }

        public static void SetMaxRetries(this OptionsConfigurationBuilder builder, int maxRetries)
        {
            builder.Registrar.RegisterOptionConfig(o => o.MaxRetries = maxRetries);
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
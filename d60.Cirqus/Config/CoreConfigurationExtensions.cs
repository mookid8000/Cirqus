using System;
using d60.Cirqus.Aggregates;
using d60.Cirqus.Config.Configurers;
using d60.Cirqus.Events;
using d60.Cirqus.Exceptions;
using d60.Cirqus.Logging;
using d60.Cirqus.Logging.Console;
using d60.Cirqus.Logging.Null;
using d60.Cirqus.Snapshotting;
using d60.Cirqus.Views;
using d60.Cirqus.Views.ViewManagers;
using ViewManagerEventDispatcher = d60.Cirqus.Views.ViewManagerEventDispatcher;

namespace d60.Cirqus.Config
{
    public static class CoreConfigurationExtensions
    {
        /// <summary>
        /// Registers a <see cref="CachingAggregateRootRepositoryDecorator"/> as a decorator in front of the existing <see cref="IAggregateRootRepository"/>
        /// which will use an <see cref="InMemorySnapshotCache"/> to cache aggregate roots.
        /// </summary>
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

        /// <summary>
        /// Registers a <see cref="DefaultAggregateRootRepository"/> as the <see cref="IAggregateRootRepository"/> implementation. Since this is the
        /// default, there's no need to call this method explicitly.
        /// </summary>
        public static void UseDefault(this AggregateRootRepositoryConfigurationBuilder builder)
        {
            builder.Registrar
                .Register<IAggregateRootRepository>(context => new DefaultAggregateRootRepository(context.Get<IEventStore>()));
        }

        /// <summary>
        /// Registers a <see cref="Views.ViewManagerEventDispatcher"/> to manage the given views. Can be called multiple times in order to register
        /// multiple "pools" of views (each will be managed by a dedicated worker thread).
        /// </summary>
        public static void UseViewManagerEventDispatcher(this EventDispatcherConfigurationBuilder builder, params IViewManager[] viewManagers)
        {
            AddEventDispatcherRegistration(builder, context => new ViewManagerEventDispatcher(
                context.Get<IAggregateRootRepository>(),
                context.Get<IEventStore>(), viewManagers));
        }

        /// <summary>
        /// Registers a <see cref="ViewManagerEventDispatcher"/> to manage the given views. Can be called multiple times in order to register
        /// multiple "pools" of views (each will be managed by a dedicated worker thread). The event dispatcher will register itself with the
        /// given <seealso cref="waitHandle"/>, allowing for optionally blocking until views have been updated to a certain point.
        /// </summary>
        public static void UseViewManagerEventDispatcher(this EventDispatcherConfigurationBuilder builder, ViewManagerWaitHandle waitHandle, params IViewManager[] viewManagers)
        {
            AddEventDispatcherRegistration(builder, context =>
            {
                var eventDispatcher = new ViewManagerEventDispatcher(
                    context.Get<IAggregateRootRepository>(),
                    context.Get<IEventStore>(), viewManagers);

                waitHandle.Register(eventDispatcher);

                return eventDispatcher;
            });
        }

        /// <summary>
        /// Registers the given event dispatcher. Can be called multiple times.
        /// </summary>
        public static void UseEventDispatcher(this EventDispatcherConfigurationBuilder builder, IEventDispatcher eventDispatcher)
        {
            AddEventDispatcherRegistration(builder, context => eventDispatcher);
        }

        /// <summary>
        /// Registers the given <see cref="IEventDispatcher"/> func, using a <see cref="CompositeEventDispatcher"/> to compose with
        /// previously registered event dispatchers.
        /// </summary>
        static void AddEventDispatcherRegistration(EventDispatcherConfigurationBuilder builder, Func<ResolutionContext, IEventDispatcher> eventDispatcherFunc)
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

        /// <summary>
        /// Configures Cirqus to purge all views when initializing.
        /// </summary>
        public static void PurgeExistingViews(this OptionsConfigurationBuilder builder, bool purgeViewsAtStartup = false)
        {
            builder.Registrar.RegisterInstance<Action<Options>>(o => o.PurgeExistingViews = purgeViewsAtStartup, multi: true);
        }

        /// <summary>
        /// Registers the given exception type as a special "domain exception", which will be passed uncaught out from
        /// command processing. All other exceptions will be wrapped in a <see cref="CommandProcessingException"/>.
        /// </summary>
        public static void AddDomainExceptionType<TException>(this OptionsConfigurationBuilder builder) where TException : Exception
        {
            builder.Registrar.RegisterInstance<Action<Options>>(o => o.AddDomainExceptionType<TException>(), multi: true);
        }

        /// <summary>
        /// Configures the number of retries to perform in the event that a <see cref="ConcurrencyException"/> occurs.
        /// </summary>
        public static void SetMaxRetries(this OptionsConfigurationBuilder builder, int maxRetries)
        {
            builder.Registrar.RegisterInstance<Action<Options>>(o => o.MaxRetries = maxRetries, multi: true);
        }

        /// <summary>
        /// Configures Cirqus to log using the console.
        /// </summary>
        public static void UseConsole(this LoggingConfigurationBuilder builder, Logger.Level minLevel = Logger.Level.Info)
        {
            CirqusLoggerFactory.Current = new ConsoleLoggerFactory(minLevel: minLevel);
        }

        /// <summary>
        /// Configures Cirqus to not log anything at all.
        /// </summary>
        public static void None(this LoggingConfigurationBuilder builder)
        {
            CirqusLoggerFactory.Current = new NullLoggerFactory();
        }
    }
}
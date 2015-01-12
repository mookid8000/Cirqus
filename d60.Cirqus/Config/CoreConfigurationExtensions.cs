using System;
using d60.Cirqus.Aggregates;
using d60.Cirqus.Commands;
using d60.Cirqus.Config.Configurers;
using d60.Cirqus.Events;
using d60.Cirqus.Exceptions;
using d60.Cirqus.Logging;
using d60.Cirqus.Logging.Console;
using d60.Cirqus.Logging.Null;
using d60.Cirqus.Serialization;
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
                        context.Get<IEventStore>(),
                        context.Get<IDomainEventSerializer>()),
                    decorator: true
                );
        }

        /// <summary>
        /// Enables an in-memory event cache that caches the most recently used events. <see cref="maxCacheEntries"/> specifies
        /// the approximate number of events to be held in the cache
        /// </summary>
        public static void EnableCaching(this EventStoreConfigurationBuilder builder, int maxCacheEntries)
        {
            builder.Registrar
                .Register<IEventStore>(context => new CachingEventStoreDecorator(context.Get<IEventStore>()),
                    decorator: true);
        }

        /// <summary>
        /// Registers a <see cref="DefaultAggregateRootRepository"/> as the <see cref="IAggregateRootRepository"/> implementation. Since this is the
        /// default, there's no need to call this method explicitly.
        /// </summary>
        public static void UseDefault(this AggregateRootRepositoryConfigurationBuilder builder)
        {
            builder.Registrar
                .Register<IAggregateRootRepository>(context =>
                {
                    var eventStore = context.Get<IEventStore>();
                    var domainEventSerializer = context.Get<IDomainEventSerializer>();
                    var domainTypeNameMapper = context.Get<IDomainTypeNameMapper>();

                    return new DefaultAggregateRootRepository(eventStore, domainEventSerializer, domainTypeNameMapper);
                });
        }

        /// <summary>
        /// Registers a <see cref="FactoryBasedAggregateRootRepository"/> as the <see cref="IAggregateRootRepository"/> implementation. 
        /// </summary>
        public static void UseFactoryMethod(this AggregateRootRepositoryConfigurationBuilder builder, Func<Type, AggregateRoot> factoryMethod)
        {
            builder.Registrar
                .Register<IAggregateRootRepository>(context =>
                {
                    var eventStore = context.Get<IEventStore>();
                    var domainEventSerializer = context.Get<IDomainEventSerializer>();
                    var domainTypeNameMapper = context.Get<IDomainTypeNameMapper>();

                    return new FactoryBasedAggregateRootRepository(eventStore, domainEventSerializer, domainTypeNameMapper, factoryMethod);
                });
        }

        /// <summary>
        /// Registers a <see cref="Views.ViewManagerEventDispatcher"/> to manage the given views. Can be called multiple times in order to register
        /// multiple "pools" of views (each will be managed by a dedicated worker thread).
        /// </summary>
        public static ViewManagerEventDispatcherConfiguationBuilder UseViewManagerEventDispatcher(this EventDispatcherConfigurationBuilder builder, params IViewManager[] viewManagers)
        {
            var viewManagerConfigurationContainer = new ConfigurationContainer();

            AddEventDispatcherRegistration(builder, context =>
            {
                var eventDispatcher = new ViewManagerEventDispatcher(
                    context.Get<IAggregateRootRepository>(),
                    context.Get<IEventStore>(),
                    context.Get<IDomainEventSerializer>(),
                    context.Get<IDomainTypeNameMapper>(),
                    viewManagers);

                var viewManagerContext = viewManagerConfigurationContainer.CreateContext();
                
                var waitHandle = viewManagerContext.GetOrDefault<ViewManagerWaitHandle>();
                if (waitHandle != null)
                {
                    waitHandle.Register(eventDispatcher);
                }

                var maxDomainEventsPerBatch = viewManagerContext.GetOrDefault<int>();
                if (maxDomainEventsPerBatch > 0)
                {
                    eventDispatcher.MaxDomainEventsPerBatch = maxDomainEventsPerBatch;
                }

                return eventDispatcher;
            });

            return new ViewManagerEventDispatcherConfiguationBuilder(viewManagerConfigurationContainer);
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
            builder.AddEventDispatcher(eventDispatcherFunc);
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
        /// Registers the given domain even serializer to be used instead of the default <see cref="JsonDomainEventSerializer"/>.
        /// </summary>
        public static void UseCustomDomainEventSerializer(this OptionsConfigurationBuilder builder, IDomainEventSerializer domainEventSerializer)
        {
            builder.Registrar.RegisterInstance(domainEventSerializer);
        }

        /// <summary>
        /// Registers the given type name mapper to be used instead of the default <see cref="DefaultDomainTypeNameMapper"/>
        /// </summary>
        public static void UseCustomDomainTypeNameMapper(this OptionsConfigurationBuilder builder, IDomainTypeNameMapper domainTypeNameMapper)
        {
            builder.Registrar.RegisterInstance(domainTypeNameMapper);
        }

        /// <summary>
        /// Configures the number of retries to perform in the event that a <see cref="ConcurrencyException"/> occurs.
        /// </summary>
        public static void SetMaxRetries(this OptionsConfigurationBuilder builder, int maxRetries)
        {
            builder.Registrar.RegisterInstance<Action<Options>>(o => o.MaxRetries = maxRetries, multi: true);
        }

        /// <summary>
        /// Decorates the <see cref="ICommandMapper"/> pipeline with a command mapper that can use the given <see cref="CommandMappings"/>
        /// </summary>
        public static void AddCommandMappings(this OptionsConfigurationBuilder builder, CommandMappings mappings)
        {
            builder.Registrar.Register(c => mappings.CreateCommandMapperDecorator(c.Get<ICommandMapper>()), decorator: true);
        }

        /// <summary>
        /// Configures Cirqus to log using the console.
        /// </summary>
        public static void UseConsole(this LoggingConfigurationBuilder builder, Logger.Level minLevel = Logger.Level.Info)
        {
            builder.Use(new ConsoleLoggerFactory(minLevel: minLevel));
        }

        /// <summary>
        /// Configures Cirqus to not log anything at all.
        /// </summary>
        public static void None(this LoggingConfigurationBuilder builder)
        {
            builder.Use(new NullLoggerFactory());
        }

        /// <summary>
        /// Configures Cirqus get its logger using specified factory.
        /// </summary>
        public static void Use(this LoggingConfigurationBuilder builder, CirqusLoggerFactory factory)
        {
            CirqusLoggerFactory.Current = factory;
        }
    }
}
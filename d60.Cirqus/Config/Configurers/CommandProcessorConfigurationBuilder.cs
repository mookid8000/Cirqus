using System;
using System.Collections.Generic;
using System.Linq;
using d60.Cirqus.Aggregates;
using d60.Cirqus.Events;

namespace d60.Cirqus.Config.Configurers
{
    class CommandProcessorConfigurationBuilder :
        ILoggingAndEventStoreConfigurationBuilderApi,
        IAggregateRootRepositoryConfigurationBuilderApi,
        IEventDispatcherConfigurationBuilderApi,
        IFullConfiguration,
        IRegistrar
    {
        readonly List<ResolutionContext.Resolver> _resolvers = new List<ResolutionContext.Resolver>();
        readonly List<Action<Options>> _optionActions = new List<Action<Options>>();

        public IAggregateRootRepositoryConfigurationBuilderApi EventStore(Action<EventStoreConfigurationBuilder> configure)
        {
            configure(new EventStoreConfigurationBuilder(this));
            return this;
        }

        public IEventStoreConfigurationBuilderApi Logging(Action<LoggingConfigurationBuilder> configure)
        {
            configure(new LoggingConfigurationBuilder(this));
            return this;
        }

        public IEventDispatcherConfigurationBuilderApi AggregateRootRepository(Action<AggregateRootRepositoryConfigurationBuilder> configure)
        {
            configure(new AggregateRootRepositoryConfigurationBuilder(this));
            return this;
        }

        public IFullConfiguration EventDispatcher(Action<EventDispatcherConfigurationBuilder> configure)
        {
            configure(new EventDispatcherConfigurationBuilder(this));
            return this;
        }

        public IFullConfiguration Options(Action<OptionsConfigurationBuilder> configure)
        {
            configure(new OptionsConfigurationBuilder(this));
            return this;
        }

        public ICommandProcessor Create()
        {
            FillInDefaults();

            var resolutionContext = new ResolutionContext(_resolvers);

            var eventStore = resolutionContext.Get<IEventStore>();
            var aggregateRootRepository = resolutionContext.Get<IAggregateRootRepository>();
            var eventDispatcher = resolutionContext.Get<IEventDispatcher>();

            var commandProcessor = new CommandProcessor(eventStore, aggregateRootRepository, eventDispatcher);

            _optionActions.ForEach(action => action(commandProcessor.Options));

            commandProcessor.Initialize();

            return commandProcessor;
        }

        void FillInDefaults()
        {
            if (_resolvers.OfType<ResolutionContext.Resolver<IAggregateRootRepository>>().All(r => r.Decorator))
            {
                Register<IAggregateRootRepository>(context => new DefaultAggregateRootRepository(context.Get<IEventStore>()));
            }
        }

        public void Register<TService>(Func<ResolutionContext, TService> serviceFactory, bool decorator = false)
        {
            var havePrimaryResolverAlready = _resolvers.OfType<ResolutionContext.Resolver<TService>>().Any(r => !r.Decorator);

            if (!decorator && havePrimaryResolverAlready)
            {
                var message = string.Format("Attempted to register factory method for {0} as non-decorator," +
                                            " but there's already a primary resolver for that service! There" +
                                            " can be only one primary resolver for each service type (but" +
                                            " any number of decorators)",
                    typeof(TService));

                throw new InvalidOperationException(message);
            }

            var resolver = new ResolutionContext.Resolver<TService>
            {
                Type = typeof (TService),
                Factory = serviceFactory,
                Decorator = decorator
            };

            if (decorator)
            {
                _resolvers.Insert(0, resolver);
                return;
            }
            
            _resolvers.Add(resolver);
        }

        public void RegisterOptionConfig(Action<Options> optionAction)
        {
            _optionActions.Add(optionAction);
        }
    }
}
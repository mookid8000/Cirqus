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
            var resolutionContext = new ResolutionContext(_factories);

            var eventStore = Get<IEventStore>(resolutionContext);
            var aggregateRootRepository = Get<IAggregateRootRepository>(resolutionContext);
            var eventDispatcher = Get<IEventDispatcher>(resolutionContext);

            var commandProcessor = new CommandProcessor(eventStore, aggregateRootRepository, eventDispatcher);
            
            _factories
                .OfType<Action<Options>>()
                .ToList()
                .ForEach(action => action(commandProcessor.Options));

            commandProcessor.Initialize();

            return commandProcessor;
        }

        readonly List<Delegate> _factories = new List<Delegate>();

        public void Register<TService>(Func<ResolutionContext, TService> serviceFactory, bool decorator = false)
        {
            if (decorator)
            {
                _factories.Insert(0, serviceFactory);
                return;
            }

            _factories.Add(serviceFactory);
        }

        public TService Get<TService>(ResolutionContext context)
        {
            return context.Get<TService>();
        }

        public void RegisterOptionConfig(Action<Options> optionAction)
        {
            _factories.Add(optionAction);
        }
    }
}
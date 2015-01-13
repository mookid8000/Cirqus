using System;
using System.Linq;
using d60.Cirqus.Aggregates;
using d60.Cirqus.Commands;
using d60.Cirqus.Events;
using d60.Cirqus.Serialization;
using d60.Cirqus.Views;

namespace d60.Cirqus.Config.Configurers
{
    internal class CommandProcessorConfigurationBuilder : ILoggingAndEventStoreConfiguration, IOptionalConfiguration<ICommandProcessor>
    {
        readonly ConfigurationContainer _container = new ConfigurationContainer();

        public IEventStoreConfiguration Logging(Action<LoggingConfigurationBuilder> configure)
        {
            configure(new LoggingConfigurationBuilder(_container));
            return this;
        }

        public IOptionalConfiguration<ICommandProcessor> EventStore(Action<EventStoreConfigurationBuilder> configure)
        {
            configure(new EventStoreConfigurationBuilder(_container));
            return this;
        }

        public IOptionalConfiguration<ICommandProcessor> AggregateRootRepository(Action<AggregateRootRepositoryConfigurationBuilder> configure)
        {
            configure(new AggregateRootRepositoryConfigurationBuilder(_container));
            return this;
        }

        public IOptionalConfiguration<ICommandProcessor> EventDispatcher(Action<EventDispatcherConfigurationBuilder> configure)
        {
            configure(new EventDispatcherConfigurationBuilder(_container));
            return this;
        }

        public IOptionalConfiguration<ICommandProcessor> Options(Action<OptionsConfigurationBuilder> configure)
        {
            configure(new OptionsConfigurationBuilder(_container));
            return this;
        }

        public ICommandProcessor Create()
        {
            FillInDefaults();

            var resolutionContext = _container.CreateContext();

            var eventStore = resolutionContext.Get<IEventStore>();
            var aggregateRootRepository = resolutionContext.Get<IAggregateRootRepository>();
            var eventDispatcher = resolutionContext.Get<IEventDispatcher>();
            var serializer = resolutionContext.Get<IDomainEventSerializer>();
            var commandMapper = resolutionContext.Get<ICommandMapper>();
            var domainTypeMapper = resolutionContext.Get<IDomainTypeNameMapper>();

            var commandProcessor = new CommandProcessor(eventStore, aggregateRootRepository, eventDispatcher, serializer, commandMapper, domainTypeMapper);

            // end the resolution context and dispose burdens when command processor is disposed
            commandProcessor.Disposed += resolutionContext.Dispose;

            resolutionContext.GetAll<Action<Options>>()
                .ToList()
                .ForEach(action => action(commandProcessor.Options));

            commandProcessor.Initialize();

            return commandProcessor;
        }

        void FillInDefaults()
        {
            if (!_container.HasService<IAggregateRootRepository>(checkForPrimary: true))
            {
                _container.Register<IAggregateRootRepository>(context =>
                    new DefaultAggregateRootRepository(
                        context.Get<IEventStore>(),
                        context.Get<IDomainEventSerializer>(),
                        context.Get<IDomainTypeNameMapper>()));
            }

            if (!_container.HasService<IDomainEventSerializer>(checkForPrimary: true))
            {
                _container.Register<IDomainEventSerializer>(context => new JsonDomainEventSerializer());
            }

            if (!_container.HasService<IEventDispatcher>(checkForPrimary: true))
            {
                _container.Register<IEventDispatcher>(context => new NullEventDispatcher());
            }

            if (!_container.HasService<ICommandMapper>(checkForPrimary: true))
            {
                _container.Register<ICommandMapper>(context => new DefaultCommandMapper());
            }

            if (!_container.HasService<IDomainTypeNameMapper>(checkForPrimary: true))
            {
                _container.Register<IDomainTypeNameMapper>(context => new DefaultDomainTypeNameMapper());
            }
        }
    }
}
using System;
using System.Configuration;
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
            var commandProcessor = resolutionContext.Get<ICommandProcessor>();

            return commandProcessor;
        }

        void FillInDefaults()
        {
            if (_container.HasService<ICommandProcessor>(checkForPrimary: true))
            {
                throw new ConfigurationErrorsException("Cannot register the real CommandProcessor because the configuration container already contains a primary registration for ICommandProcessor");
            }

            _container.Register<ICommandProcessor>(context =>
            {
                var eventStore = context.Get<IEventStore>();
                var aggregateRootRepository = context.Get<IAggregateRootRepository>();
                var eventDispatcher = context.Get<IEventDispatcher>();
                var serializer = context.Get<IDomainEventSerializer>();
                var commandMapper = context.Get<ICommandMapper>();
                var domainTypeMapper = context.Get<IDomainTypeNameMapper>();

                var options = new Options();

                context.GetAll<Action<Options>>()
                    .ToList()
                    .ForEach(action => action(options));

                var commandProcessor = new CommandProcessor(eventStore, aggregateRootRepository, eventDispatcher, serializer, commandMapper, domainTypeMapper, options);

                // end the resolution context and dispose burdens when command processor is disposed
                commandProcessor.Disposed += context.Dispose;

                commandProcessor.Initialize();

                return commandProcessor;
            });

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
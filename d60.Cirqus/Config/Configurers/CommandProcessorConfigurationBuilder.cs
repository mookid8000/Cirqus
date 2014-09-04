using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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

            var resolver = new ResolutionContext.Resolver<TService>(serviceFactory, decorator);

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

        internal void LogServicesTo(TextWriter writer)
        {
            var es = _resolvers.OfType<ResolutionContext.Resolver<IEventStore>>().ToList();
            var agg = _resolvers.OfType<ResolutionContext.Resolver<IAggregateRootRepository>>().ToList();
            var ed = _resolvers.OfType<ResolutionContext.Resolver<IEventDispatcher>>().ToList();

            writer.WriteLine(@"----------------------------------------------------------------------
Event store:
{0}

Aggregate root repository:
{1}

Event dispatcher:
{2}
----------------------------------------------------------------------",
                                                                       Format(es), Format(agg), Format(ed));
        }

        string Format<TService>(List<ResolutionContext.Resolver<TService>> agg)
        {
            var primary = agg.Where(r => !r.Decorator)
                .ToList();

            var decorators = agg.Where(r => r.Decorator)
                .ToList();

            var builder = new StringBuilder();

            if (primary.Any())
            {
                builder.AppendLine(@"    Primary:");
                builder.AppendLine(string.Join(Environment.NewLine, primary.Select(p => string.Format("        {0}", p.Type))));
            }

            if (decorators.Any())
            {
                builder.AppendLine(@"    Decorators:");
                builder.AppendLine(string.Join(Environment.NewLine, decorators.Select(p => string.Format("        {0}", p.Type))));
            }

            return builder.ToString();
        }
    }
}
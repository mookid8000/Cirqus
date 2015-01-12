using System;
using System.Linq;
using d60.Cirqus.Aggregates;
using d60.Cirqus.Commands;
using d60.Cirqus.Config;
using d60.Cirqus.Config.Configurers;
using d60.Cirqus.Events;
using d60.Cirqus.Logging;
using d60.Cirqus.Serialization;
using d60.Cirqus.Testing.Internals;
using d60.Cirqus.Views;

namespace d60.Cirqus.Testing
{
    class TestContextConfigurationBuilder : IOptionalConfiguration<TestContext>
    {
        static Logger _logger;
        readonly ConfigurationContainer _container = new ConfigurationContainer();

        static TestContextConfigurationBuilder()
        {
            CirqusLoggerFactory.Changed += f => _logger = f.GetCurrentClassLogger();
        }

        public IOptionalConfiguration<TestContext> AggregateRootRepository(Action<AggregateRootRepositoryConfigurationBuilder> configure)
        {
            configure(new AggregateRootRepositoryConfigurationBuilder(_container));
            return this;
        }

        public IOptionalConfiguration<TestContext> EventDispatcher(Action<EventDispatcherConfigurationBuilder> configure)
        {
            configure(new EventDispatcherConfigurationBuilder(_container));
            return this;
        }

        public IOptionalConfiguration<TestContext> Options(Action<OptionsConfigurationBuilder> configure)
        {
            configure(new OptionsConfigurationBuilder(_container));
            return this;
        }

        public TestContext Create()
        {
            FillInDefaults();

            var resolutionContext = _container.CreateContext();

            var eventStore = resolutionContext.Get<InMemoryEventStore>();
            var aggregateRootRepository = resolutionContext.Get<IAggregateRootRepository>();
            var eventDispatcher = resolutionContext.Get<IEventDispatcher>();
            var serializer = resolutionContext.Get<IDomainEventSerializer>();
            var commandMapper = resolutionContext.Get<ICommandMapper>();
            var domainTypeMapper = resolutionContext.Get<IDomainTypeNameMapper>();

            var testContext = new TestContext(eventStore, aggregateRootRepository, eventDispatcher, serializer, commandMapper, domainTypeMapper);

            resolutionContext
                .GetAll<Action<TestContext>>().ToList()
                .ForEach(action => action(testContext)); 

            testContext.Initialize();

            return testContext;
        }

        void FillInDefaults()
        {
            _container.Register(x => new InMemoryEventStore(x.Get<IDomainEventSerializer>()));
            _container.Register<IEventStore>(x => x.Get<InMemoryEventStore>());

            if (!_container.HasService<IEventDispatcher>(checkForPrimary: true))
            {
                _container.Register<IEventDispatcher>(x =>
                    new ViewManagerEventDispatcher(
                        x.Get<IAggregateRootRepository>(),
                        x.Get<IEventStore>(),
                        x.Get<IDomainEventSerializer>(),
                        x.Get<IDomainTypeNameMapper>()));
            }

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
                _container.Register<IDomainEventSerializer>(context => new JsonDomainEventSerializer("<events>"));
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
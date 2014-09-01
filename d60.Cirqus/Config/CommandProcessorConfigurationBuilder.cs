using System;
using System.Collections.Generic;
using System.Linq;
using d60.Cirqus.Aggregates;
using d60.Cirqus.Events;

namespace d60.Cirqus.Config
{
    class CommandProcessorConfigurationBuilder :
        IEventStoreConfigurationBuilderApi,
        IAggregateRootRepositoryConfigurationBuilderApi,
        IEventDispatcherConfigurationBuilderApi,
        IFullConfiguration,
        IServiceRegistrar
    {
        public IAggregateRootRepositoryConfigurationBuilderApi EventStore(Action<EventStoreConfigurationBuilder> configure)
        {
            configure(new EventStoreConfigurationBuilder(this));
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
            var commandProcessor = new CommandProcessor(Get<IEventStore>(), Get<IAggregateRootRepository>(), Get<IEventDispatcher>());
            
            _factories
                .OfType<Action<Options>>()
                .ToList()
                .ForEach(action => action(commandProcessor.Options));

            commandProcessor.Initialize();

            return commandProcessor;
        }

        readonly List<Delegate> _factories = new List<Delegate>();

        public void Register<TService>(Func<TService> serviceFactory)
        {
            _factories.Add(serviceFactory);
        }

        public TService Get<TService>()
        {
            var matchingFactoryMethod = _factories.OfType<Func<TService>>().FirstOrDefault();

            if (matchingFactoryMethod == null)
            {
                throw new InvalidOperationException(string.Format("Cannot provide an instance of {0} because an appropriate factory method has not been registered!", typeof(TService)));
            }

            return matchingFactoryMethod();
        }

        public void RegisterOptionConfig(Action<Options> optionAction)
        {
            _factories.Add(optionAction);
        }
    }

    public interface IFullConfiguration
    {
        IFullConfiguration Options(Action<OptionsConfigurationBuilder> func);
        ICommandProcessor Create();
    }

    public interface IEventStoreConfigurationBuilderApi
    {
        IAggregateRootRepositoryConfigurationBuilderApi EventStore(Action<EventStoreConfigurationBuilder> configure);
    }

    public interface IAggregateRootRepositoryConfigurationBuilderApi
    {
        IEventDispatcherConfigurationBuilderApi AggregateRootRepository(Action<AggregateRootRepositoryConfigurationBuilder> configure);
    }

    public interface IEventDispatcherConfigurationBuilderApi
    {
        IFullConfiguration EventDispatcher(Action<EventDispatcherConfigurationBuilder> configure);
    }

    public interface IFullConfigurationConfigurationBuilderApi
    {
        IFullConfiguration Options(Action<OptionsConfigurationBuilder> configure);
    }

    public class EventStoreConfigurationBuilder
    {
        readonly IServiceRegistrar _serviceRegistrar;

        public EventStoreConfigurationBuilder(IServiceRegistrar serviceRegistrar)
        {
            _serviceRegistrar = serviceRegistrar;
        }

        public IServiceRegistrar ServiceRegistrar
        {
            get { return _serviceRegistrar; }
        }
    }

    public class AggregateRootRepositoryConfigurationBuilder
    {
        readonly IServiceRegistrar _serviceRegistrar;

        public AggregateRootRepositoryConfigurationBuilder(IServiceRegistrar serviceRegistrar)
        {
            _serviceRegistrar = serviceRegistrar;
        }

        public IServiceRegistrar ServiceRegistrar
        {
            get { return _serviceRegistrar; }
        }
    }

    public class EventDispatcherConfigurationBuilder
    {
        readonly IServiceRegistrar _serviceRegistrar;

        public EventDispatcherConfigurationBuilder(IServiceRegistrar serviceRegistrar)
        {
            _serviceRegistrar = serviceRegistrar;
        }

        public IServiceRegistrar ServiceRegistrar
        {
            get { return _serviceRegistrar; }
        }
    }

    public class OptionsConfigurationBuilder
    {
        readonly IServiceRegistrar _serviceRegistrar;

        public IServiceRegistrar ServiceRegistrar
        {
            get { return _serviceRegistrar; }
        }

        public OptionsConfigurationBuilder(IServiceRegistrar serviceRegistrar)
        {
            _serviceRegistrar = serviceRegistrar;
        }
    }

    public interface IServiceRegistrar
    {
        void Register<TService>(Func<TService> serviceFactory);
        TService Get<TService>();
        void RegisterOptionConfig(Action<Options> optionAction);
    }
}
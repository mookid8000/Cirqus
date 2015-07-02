using System;
using d60.Cirqus.Aggregates;
using d60.Cirqus.Events;
using d60.Cirqus.Serialization;
using d60.Cirqus.Views;
using d60.Cirqus.Views.ViewManagers;

namespace d60.Cirqus.Config.Configurers
{
    /// <summary>
    /// Configuration builder for attaching event processors to the command processor
    /// </summary>
    public class EventDispatcherConfigurationBuilder : ConfigurationBuilder<IEventDispatcher>
    {
        /// <summary>
        /// Constructs the builder
        /// </summary>
        public EventDispatcherConfigurationBuilder(IRegistrar registrar) : base(registrar) { }

        /// <summary>
        /// Registers a <see cref="Views.ViewManagerEventDispatcher"/> to manage the given views. Can be called multiple times in order to register
        /// multiple "pools" of views (each will be managed by a dedicated worker thread).
        /// </summary>
        public ViewManagerEventDispatcherConfiguationBuilder UseViewManagerEventDispatcher(params IViewManager[] viewManagers)
        {
            var viewManagerConfigurationContainer = new ConfigurationContainer();

            UseEventDispatcher(context =>
            {
                var viewManagerContext = viewManagerConfigurationContainer.CreateContext();

                context.AddChildContext(viewManagerContext);

                var eventDispatcher = new ViewManagerEventDispatcher(
                    context.Get<IAggregateRootRepository>(),
                    context.Get<IEventStore>(),
                    context.Get<IDomainEventSerializer>(),
                    context.Get<IDomainTypeNameMapper>(),
                    viewManagers);

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

                var viewManagerProfiler = viewManagerContext.GetOrDefault<IViewManagerProfiler>();
                if (viewManagerProfiler != null)
                {
                    eventDispatcher.SetProfiler(viewManagerProfiler);
                }

                return eventDispatcher;
            });

            return new ViewManagerEventDispatcherConfiguationBuilder(viewManagerConfigurationContainer);
        }

        /// <summary>
        /// Configures a dependent view manager event dispatcher that tacks on to any number of dependent views, catching up from the
        /// event store when the dependent views have caught up.
        /// </summary>
        public DependentViewManagerEventDispatcherSettings UseDependentViewManagerEventDispatcher(params IViewManager[] viewManagers)
        {
            var settings = new DependentViewManagerEventDispatcherSettings();

            UseEventDispatcher(context =>
            {
                var eventDispatcher = new DependentViewManagerEventDispatcher(settings.DependentViewManagers,
                    viewManagers,
                    context.Get<IEventStore>(),
                    context.Get<IDomainEventSerializer>(),
                    context.Get<IAggregateRootRepository>(),
                    context.Get<IDomainTypeNameMapper>(),
                    settings.ViewContextItems)
                {
                    MaxDomainEventsPerBatch = settings.MaxDomainEventsPerBatch
                };

                if (settings.ViewManagerProfiler != null)
                {
                    eventDispatcher.SetProfiler(settings.ViewManagerProfiler);
                }

                foreach (var waitHandle in settings.WaitHandles)
                {
                    waitHandle.Register(eventDispatcher);
                }

                return eventDispatcher;
            });

            return settings;
        }

        /// <summary>
        /// Registers the given event dispatcher. Can be called multiple times.
        /// </summary>
        public void UseEventDispatcher(IEventDispatcher eventDispatcher)
        {
            UseEventDispatcher(context => eventDispatcher);
        }

        /// <summary>
        /// Registers the given <see cref="IEventDispatcher"/> func, using a <see cref="CompositeEventDispatcher"/> to compose with
        /// previously registered event dispatchers.
        /// </summary>
        public void UseEventDispatcher(Func<ResolutionContext, IEventDispatcher> factory)
        {
            if (HasService<IEventDispatcher>())
            {
                Decorate(context =>
                    new CompositeEventDispatcher(
                        context.Get<IEventDispatcher>(),
                        factory(context)));
            }
            else
            {
                Register(factory);
            }
        }
    }
}
using System.Collections.Generic;
using d60.Cirqus.Aggregates;
using d60.Cirqus.Config;
using d60.Cirqus.Config.Configurers;
using d60.Cirqus.Events;
using d60.Cirqus.Serialization;
using d60.Cirqus.Views;

namespace d60.Cirqus.Testing
{
    public static class TestingConfigurationExtensions
    {
        public static SynchronousViewManagerEventDispatcherConfiguationBuilder UseSynchronousViewManangerEventDispatcher(
            this EventDispatcherConfigurationBuilder builder, params IViewManager[] viewManagers)
        {
            var viewManagerConfigurationContainer = new ConfigurationContainer();

            builder.UseEventDispatcher(context =>
            {
                var viewManagerContext = viewManagerConfigurationContainer.CreateContext();

                context.AddChildContext(viewManagerContext);

                var eventDispatcher = new SynchronousViewManagerEventDispatcher(
                    context.Get<IEventStore>(),
                    context.Get<IAggregateRootRepository>(),
                    context.Get<IDomainEventSerializer>(),
                    context.Get<IDomainTypeNameMapper>(),
                    viewManagers);

                var contextItems = viewManagerContext.GetOrDefault<IDictionary<string, object>>();
                if (contextItems != null)
                {
                    eventDispatcher.SetContextItems(contextItems);
                }

                return eventDispatcher;
            });

            return new SynchronousViewManagerEventDispatcherConfiguationBuilder(viewManagerConfigurationContainer);
        }
    }
}
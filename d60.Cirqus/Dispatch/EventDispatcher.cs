using d60.Cirqus.Aggregates;
using d60.Cirqus.Config.Configurers;
using d60.Cirqus.Events;
using d60.Cirqus.Views;
using d60.Cirqus.Views.ViewManagers;

namespace d60.Cirqus.Dispatch
{
    public class EventDispatcher
    {
        public static StandAloneEventDispatcherConfigurationBuilder With()
        {
            return new StandAloneEventDispatcherConfigurationBuilder();
        } 
    }

    public class StandAloneEventDispatcherConfigurationBuilder
    {
        readonly ConfigurationContainer _container = new ConfigurationContainer();
        readonly ViewManagerWaitHandle _viewManagerWaitHandle = new ViewManagerWaitHandle();

        public IRegistrar Registrar
        {
            get { return _container; }
        }

        public StandAloneEventDispatcherConfigurationBuilder ViewManager(params IManagedView[] managedViews)
        {
            if (_container.HasService<IEventDispatcher>())
            {
                _container.Register(c => new CompositeEventDispatcher(c.Get<IEventDispatcher>(),
                    new ViewManagerEventDispatcher(c.Get<IAggregateRootRepository>(), c.Get<IEventStore>(), managedViews)));
            }
            else
            {
                _container.Register(c => new ViewManagerEventDispatcher(c.Get<IAggregateRootRepository>(), c.Get<IEventStore>(), managedViews));
            }
            return this;
        }
    }
}
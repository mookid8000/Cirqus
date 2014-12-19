using System;
using d60.Cirqus.Views;

namespace d60.Cirqus.Config.Configurers
{
    public class EventDispatcherConfigurationBuilder : ConfigurationBuilder
    {
        public EventDispatcherConfigurationBuilder(IRegistrar registrar) : base(registrar) { }

        public void AddEventDispatcher(Func<ResolutionContext, IEventDispatcher> eventDispatcherFunc)
        {
            if (Registrar.HasService<IEventDispatcher>())
            {
                Register<IEventDispatcher>(context => new CompositeEventDispatcher(context.Get<IEventDispatcher>(), eventDispatcherFunc(context)), decorator: true);
            }
            else
            {
                Register(eventDispatcherFunc);
            }

        }
    }
}
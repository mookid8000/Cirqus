using System;
using d60.Cirqus.Views;

namespace d60.Cirqus.Config.Configurers
{
    public class EventDispatcherConfigurationBuilder
    {
        readonly IRegistrar _registrar;

        public EventDispatcherConfigurationBuilder(IRegistrar registrar)
        {
            _registrar = registrar;
        }

        public IRegistrar Registrar
        {
            get { return _registrar; }
        }

        public void AddEventDispatcher(Func<ResolutionContext, IEventDispatcher> eventDispatcherFunc)
        {
            if (Registrar.HasService<IEventDispatcher>())
            {
                Registrar
                    .Register<IEventDispatcher>(context => new CompositeEventDispatcher(context.Get<IEventDispatcher>(), eventDispatcherFunc(context)), decorator: true);
            }
            else
            {
                Registrar.Register(eventDispatcherFunc);
            }

        }
    }
}
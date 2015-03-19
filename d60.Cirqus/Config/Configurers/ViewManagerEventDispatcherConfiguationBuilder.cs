using d60.Cirqus.Views;
using d60.Cirqus.Views.ViewManagers;

namespace d60.Cirqus.Config.Configurers
{
    public class ViewManagerEventDispatcherConfiguationBuilder : ConfigurationBuilder<ViewManagerEventDispatcher>
    {
        public ViewManagerEventDispatcherConfiguationBuilder(IRegistrar registrar) : base(registrar) {}

        public ViewManagerEventDispatcherConfiguationBuilder WithWaitHandle(ViewManagerWaitHandle handle)
        {
            RegisterInstance(handle);
            return this;
        }

        public ViewManagerEventDispatcherConfiguationBuilder WithMaxDomainEventsPerBatch(int max)
        {
            RegisterInstance(max);
            return this;
        }

        public ViewManagerEventDispatcherConfiguationBuilder WithProfiler(IViewManagerProfiler profiler)
        {
            RegisterInstance(profiler);
            return this;
        }
    }
}
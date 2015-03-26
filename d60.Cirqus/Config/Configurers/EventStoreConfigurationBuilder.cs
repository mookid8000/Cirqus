using d60.Cirqus.Events;

namespace d60.Cirqus.Config.Configurers
{
    public class EventStoreConfigurationBuilder : ConfigurationBuilder<IEventStore>
    {
        public EventStoreConfigurationBuilder(IRegistrar registrar) : base(registrar) { }

        /// <summary>
        /// Enables an in-memory event cache that caches the most recently used events. <see cref="maxCacheEntries"/> specifies
        /// the approximate number of events to be held in the cache
        /// </summary>
        public void EnableCaching(int maxCacheEntries)
        {
            Decorate(context => new CachingEventStoreDecorator(context.Get<IEventStore>())
            {
                MaxCacheEntries = maxCacheEntries
            });
        }
    }
}
using d60.Cirqus.Events;
using d60.Cirqus.Extensions;
using d60.Cirqus.Serialization;

namespace d60.Cirqus.Caching
{
    public class CachingDomainEventSerializerDecorator : IDomainEventSerializer
    {
        readonly IDomainEventSerializer _innerDomainEventSerializer;
        readonly EventCache _eventCache;

        public CachingDomainEventSerializerDecorator(IDomainEventSerializer innerDomainEventSerializer, EventCache eventCache)
        {
            _innerDomainEventSerializer = innerDomainEventSerializer;
            _eventCache = eventCache;
        }

        public EventData Serialize(DomainEvent e)
        {
            var eventData = _innerDomainEventSerializer.Serialize(e);

            // can't do this because there's no global sequence number on the event
            //_eventCache.AddToCache(e);

            return eventData;
        }

        public DomainEvent Deserialize(EventData e)
        {
            var cachedDomainEvent = _eventCache.GetDomainEvent(e.GetGlobalSequenceNumber());
            if (cachedDomainEvent != null) return cachedDomainEvent;
            
            var domainEvent = _innerDomainEventSerializer.Deserialize(e);
            _eventCache.AddToCache(domainEvent);
            return domainEvent;
        }
    }
}
using System;
using d60.Cirqus.Events;

namespace d60.Cirqus.Caching
{
    public class EventCache
    {
        public EventData GetCachedEvent(long globalSequenceNumber)
        {
            return null;
        }

        public EventData GetCachedEvent(string aggregateRootId, long sequenceNumber)
        {
            return null;
        }

        public DomainEvent GetDomainEvent(long globalSequenceNumber)
        {
            return null;
        }

        public void AddToCache(EventData eventData)
        {
            
        }

        public void AddToCache(DomainEvent domainEvent)
        {
            
        }

        class CacheEntry
        {
            public CacheEntry(EventData eventData)
            {
                EventData = eventData;
                MarkAsAccessed();
            }

            public DateTime LastAccess { get; private set; }

            public EventData EventData { get; private set; }

            public CacheEntry MarkAsAccessed()
            {
                LastAccess = DateTime.UtcNow;
                return this;
            }

            public TimeSpan Age
            {
                get { return DateTime.UtcNow - LastAccess; }
            }
        }
    }
}
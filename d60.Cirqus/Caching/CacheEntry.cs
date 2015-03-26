using System;

namespace d60.Cirqus.Caching
{
    public class CacheEntry<TData>
    {
        public CacheEntry(TData data)
        {
            Data = data;
            MarkAsAccessed();
        }

        public DateTime LastAccess { get; private set; }

        public TData Data { get; private set; }

        public CacheEntry<TData> MarkAsAccessed()
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
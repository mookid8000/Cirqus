using System;
using System.Linq;
using d60.EventSorcerer.Events;

namespace d60.EventSorcerer.Extensions
{
    public static class DomainEventExtensions
    {
        public static Guid GetAggregateRootId(this DomainEvent domainEvent, bool throwIfNotFound = true)
        {
            return GetMetadataField(domainEvent, DomainEvent.MetadataKeys.AggregateRootId, value => new Guid(Convert.ToString(value)), throwIfNotFound);
        }
        public static int GetSequenceNumber(this DomainEvent domainEvent, bool throwIfNotFound = true)
        {
            return GetMetadataField(domainEvent, DomainEvent.MetadataKeys.SequenceNumber, Convert.ToInt32, throwIfNotFound);
        }

        static TValue GetMetadataField<TValue>(DomainEvent domainEvent, string key, Func<object, TValue> converter, bool throwIfNotFound)
        {
            var metadata = domainEvent.Meta;

            if (metadata.ContainsKey(key)) return converter(metadata[key]);
            
            if (!throwIfNotFound) return converter(null);

            var metadataString = string.Join(", ", metadata.Select(kvp => string.Format("{0}: {1}", kvp.Key, kvp.Value)));
            var message = string.Format("Attempted to get value of key '{0}' from event {1}, but only the following" +
                                        " metadata were available: {2}", key, domainEvent, metadataString);

            throw new InvalidOperationException(message);
        }
    }
}
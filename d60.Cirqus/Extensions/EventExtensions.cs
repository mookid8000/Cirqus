using System;
using System.Linq;
using d60.Cirqus.Events;

namespace d60.Cirqus.Extensions
{
    public static class EventExtensions
    {
        public const string ContentTypeMetadataKey = "content-type";
        public const string Utf8JsonMetadataValue = "application/json;charset=utf8";

        /// <summary>
        /// Gets the aggregate root ID from the domain event
        /// </summary>
        public static Guid GetAggregateRootId(this Event domainEvent, bool throwIfNotFound = true)
        {
            return GetMetadataField(domainEvent, DomainEvent.MetadataKeys.AggregateRootId, value => new Guid(Convert.ToString(value)), throwIfNotFound);
        }

        /// <summary>
        /// Gets the batch ID from the domain event
        /// </summary>
        public static Guid GetBatchId(this Event domainEvent, bool throwIfNotFound = true)
        {
            return GetMetadataField(domainEvent, DomainEvent.MetadataKeys.BatchId, value => new Guid(Convert.ToString(value)), throwIfNotFound);
        }

        /// <summary>
        /// Gets the (root-local) sequence number from the domain event
        /// </summary>
        public static long GetSequenceNumber(this Event domainEvent, bool throwIfNotFound = true)
        {
            return GetMetadataField(domainEvent, DomainEvent.MetadataKeys.SequenceNumber, Convert.ToInt64, throwIfNotFound);
        }

        /// <summary>
        /// Gets the global sequence number from the domain event
        /// </summary>
        public static long GetGlobalSequenceNumber(this Event domainEvent, bool throwIfNotFound = true)
        {
            return GetMetadataField(domainEvent, DomainEvent.MetadataKeys.GlobalSequenceNumber, Convert.ToInt64, throwIfNotFound);
        }

        public static bool IsJson(this Event e)
        {
            return e.Meta.ContainsKey(ContentTypeMetadataKey)
                   && e.Meta[ContentTypeMetadataKey] == Utf8JsonMetadataValue;
        }

        public static void MarkAsJson(this Event e)
        {
            e.Meta[ContentTypeMetadataKey] = Utf8JsonMetadataValue;
        }

        static TValue GetMetadataField<TValue>(Event domainEvent, string key, Func<object, TValue> converter, bool throwIfNotFound)
        {
            var metadata = domainEvent.Meta;

            if (metadata.ContainsKey(key)) return converter(metadata[key]);

            if (!throwIfNotFound) return converter(null);

            var metadataString = string.Join(", ", metadata.Select(kvp => string.Format("{0}: {1}", kvp.Key, kvp.Value)));
            var message = string.Format("Attempted to get value of key '{0}' from event, but only the following" +
                                        " metadata were available: {1}", key, metadataString);

            throw new InvalidOperationException(message);
        }
    }
}
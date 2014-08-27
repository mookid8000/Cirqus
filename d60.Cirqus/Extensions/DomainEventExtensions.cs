using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using d60.Cirqus.Events;

namespace d60.Cirqus.Extensions
{
    public static class DomainEventExtensions
    {
        /// <summary>
        /// Gets the aggregate root ID from the domain event
        /// </summary>
        public static Guid GetAggregateRootId(this DomainEvent domainEvent, bool throwIfNotFound = true)
        {
            return GetMetadataField(domainEvent, DomainEvent.MetadataKeys.AggregateRootId, value => new Guid(Convert.ToString(value)), throwIfNotFound);
        }

        /// <summary>
        /// Gets the batch ID from the domain event
        /// </summary>
        public static Guid GetBatchId(this DomainEvent domainEvent, bool throwIfNotFound = true)
        {
            return GetMetadataField(domainEvent, DomainEvent.MetadataKeys.BatchId, value => new Guid(Convert.ToString(value)), throwIfNotFound);
        }

        /// <summary>
        /// Gets the (root-local) sequence number from the domain event
        /// </summary>
        public static long GetSequenceNumber(this DomainEvent domainEvent, bool throwIfNotFound = true)
        {
            return GetMetadataField(domainEvent, DomainEvent.MetadataKeys.SequenceNumber, Convert.ToInt64, throwIfNotFound);
        }

        /// <summary>
        /// Gets the global sequence number from the domain event
        /// </summary>
        public static long GetGlobalSequenceNumber(this DomainEvent domainEvent, bool throwIfNotFound = true)
        {
            return GetMetadataField(domainEvent, DomainEvent.MetadataKeys.GlobalSequenceNumber, Convert.ToInt64, throwIfNotFound);
        }

        public static DateTime GetUtcTime(this DomainEvent domainEvent, bool throwIfNotFound = true)
        {
            var timeAsString = GetMetadataField(domainEvent, DomainEvent.MetadataKeys.TimeUtc, Convert.ToString, throwIfNotFound);

            return DateTime.ParseExact(timeAsString, "u", CultureInfo.CurrentCulture);
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
using System;
using System.Collections.Generic;
using System.Linq;

namespace d60.Cirqus.Extensions
{
    /// <summary>
    /// Enumerable. Extensions. Yeah.
    /// </summary>
    public static class EnumerableExtensions
    {
        /// <summary>
        /// Divides the given sequence of items into batches of max <paramref name="maxItemsPerBatch"/> items per batch. Duh.
        /// </summary>
        public static IEnumerable<IEnumerable<T>> Batch<T>(this IEnumerable<T> items, int maxItemsPerBatch)
        {
            if (maxItemsPerBatch < 1)
            {
                throw new ArgumentException(string.Format("Cannot set max items per batch to {0}!", maxItemsPerBatch));
            }

            var batch = new List<T>();

            foreach (var item in items)
            {
                batch.Add(item);

                if (batch.Count < maxItemsPerBatch) continue;

                // time to return this partition and begin new one
                yield return batch;
                batch = new List<T>();
            }

            // if we've collected anything in the last partition, return it as well
            if (batch.Any())
            {
                yield return batch;
            }
        }
    }
}
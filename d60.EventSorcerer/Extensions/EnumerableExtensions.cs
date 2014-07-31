using System.Collections.Generic;
using System.Linq;

namespace d60.EventSorcerer.Extensions
{
    public static class EnumerableExtensions
    {
        public static IEnumerable<IEnumerable<T>> Batch<T>(this IEnumerable<T> items, int maxItemsPerBatch)
        {
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
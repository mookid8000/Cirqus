using System;
using System.Collections.Generic;

namespace d60.Cirqus.Extensions
{
    /// <summary>
    /// Dictionary extensions
    /// </summary>
    public static class DictionaryExtensions
    {
        public static void InsertInto<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, IDictionary<TKey, TValue> otherDictionary)
        {
            foreach (var kvp in dictionary)
            {
                otherDictionary[kvp.Key] = kvp.Value;
            }
        }

        /// <summary>
        /// Gets the value from 
        /// </summary>
        public static TValue GetOrAdd<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, Func<TKey, TValue> valueFactory)
        {
            TValue value;
            if (dictionary.TryGetValue(key, out value)) return value;

            var newValue = valueFactory(key);
            dictionary[key] = newValue;
            return newValue;
        }
    }
}
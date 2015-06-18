using System;
using System.Collections.Generic;

namespace d60.Cirqus.Extensions
{
    /// <summary>
    /// Dictionary extensions
    /// </summary>
    public static class DictionaryExtensions
    {
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
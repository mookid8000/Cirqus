using System;
using System.Collections.Generic;

namespace d60.Circus.Extensions
{
    public static class DictionaryExtensions
    {
        public static TValue GetOrAdd<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, Func<TKey, TValue> valueFactory)
        {
            if (dictionary.ContainsKey(key))
                return dictionary[key];

            var value = valueFactory(key);
            dictionary[key] = value;
            return value;
        }
    }
}
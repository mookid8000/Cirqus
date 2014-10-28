using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using d60.Cirqus.Extensions;

namespace d60.Cirqus.Numbers
{
    /// <summary>
    /// Metadata collection that stores a bunch of key-value pairs that can be used for
    /// cross-cutting concerns like e.g. handling multi-tenancy, auditing, etc.
    /// </summary>
    [Serializable]
    public sealed class Metadata : Dictionary<string, string>
    {
        public static readonly CultureInfo NumberCulture = CultureInfo.InvariantCulture;

        Metadata(SerializationInfo info, StreamingContext contest)
            : base(info, contest)
        {
        }

        public Metadata()
        {
        }

        internal void Merge(Metadata otherMeta)
        {
            foreach (var kvp in otherMeta)
            {
                if (ContainsKey(kvp.Key)) continue;

                this[kvp.Key] = kvp.Value;
            }
        }

        internal void TakeFromAttributes(ICustomAttributeProvider provider)
        {
            foreach (var meta in provider.GetAttributes<MetaAttribute>())
            {
                if (ContainsKey(meta.Key)) continue;

                this[meta.Key] = meta.Value;
            }
        }

        public override string ToString()
        {
            return string.Format("meta: ({0})",
                string.Join(", ", this.Select(kvp => string.Format(@"""{0}"": ""{1}""", kvp.Key, kvp.Value))));
        }
    }
}
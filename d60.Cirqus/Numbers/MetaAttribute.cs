using System;

namespace d60.Cirqus.Numbers
{
    /// <summary>
    /// Attribute that can be applied to events and aggregate roots. The key and value of the attribute
    /// will then be copied to the metadata of events emitted by that particular aggregate root.
    /// Can e.g. be used for stamping versioning information on events.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class MetaAttribute : Attribute
    {
        readonly string _key;
        readonly object _value;

        public MetaAttribute(string key, object value)
        {
            _key = key;
            _value = value;
        }

        public string Key
        {
            get { return _key; }
        }

        public object Value
        {
            get { return _value; }
        }

        public override string ToString()
        {
            return string.Format("{0}: {1}", _key, _value);
        }
    }
}
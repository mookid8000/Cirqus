using System;

namespace d60.EventSorcerer.Numbers
{
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
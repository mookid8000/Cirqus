using System;

namespace d60.Cirqus.Identity
{
    [AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = true)]
    public class KeyAttribute : Attribute
    {
        public KeyAttribute(string format)
        {
            Format = format;
        }

        public string Format { get; private set; }
    }
}
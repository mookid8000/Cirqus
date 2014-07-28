using System;

namespace d60.EventSorcerer.Numbers
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class VersionAttribute : Attribute
    {
        public int Number { get; private set; }
        public VersionAttribute(int number)
        {
            Number = number;
        }
    }
}
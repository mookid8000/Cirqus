using System;

namespace EnergyProjects.Domain.Model
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
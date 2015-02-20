using System;
using System.Reflection;

namespace EnergyProjects.Domain.Model
{
    public interface Id
    {
        Type ForType { get; }
    }

    public struct Id<T> : Id
    {
        private readonly string value;
        private readonly KeyFormat format;

        internal Id(KeyFormat format, string value)
        {
            AssertValid(format, value);

            this.format = format;
            this.value = value;
        }

        public override string ToString()
        {
            return value;
        }

        public Type ForType
        {
            get { return typeof(T); }
        }

        public void Apply(T target)
        {
            format.Apply(target, value);
        }

        public static implicit operator string(Id<T> id)
        {
            return id.value;
        }

        public static implicit operator Id<T>(string value)
        {
            return Parse(value);
        }

        public static Id<T> New(params object[] args)
        {
            return GetFormatFromAttribute().Compile<T>(args);
        }

        public static Id<T> Parse(string value)
        {
            return new Id<T>(GetFormatFromAttribute(), value);
        }

        static void AssertValid(KeyFormat format, string id)
        {
            if (id == null)
            {
                throw new InvalidOperationException("Id cannot be null");
            }

            if (!format.Matches(id))
            {
                throw new InvalidOperationException(string.Format(
                    "Trying to make an id for type {0}, but \"{1}\" does not match the expected format \"{2}\".", 
                    typeof(T).Name, id, format));
            }
        }

        static KeyFormat GetFormatFromAttribute()
        {
            return KeyFormat.FromAttribute(typeof (T).GetCustomAttribute<KeyAttribute>() ?? new KeyAttribute(""));
        }
    }
}
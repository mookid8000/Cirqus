using System;

namespace d60.Cirqus.TsClient.Model
{
    class QualifiedClassName : IEquatable<QualifiedClassName>
    {
        public QualifiedClassName(Type type)
        {
            Ns = type.Namespace;
            Name = type.Name;
        }

        public bool Equals(QualifiedClassName other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return string.Equals(Ns, other.Ns) && string.Equals(Name, other.Name);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((QualifiedClassName) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((Ns != null ? Ns.GetHashCode() : 0)*397) ^ (Name != null ? Name.GetHashCode() : 0);
            }
        }

        public static bool operator ==(QualifiedClassName left, QualifiedClassName right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(QualifiedClassName left, QualifiedClassName right)
        {
            return !Equals(left, right);
        }

        public string Ns { get; private set; }
 
        public string Name { get; private set; }

        public override string ToString()
        {
            return string.Format("{0} ({1})", Name, Ns);
        }
    }
}
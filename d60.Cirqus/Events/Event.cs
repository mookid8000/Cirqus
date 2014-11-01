using System;
using System.Linq;
using d60.Cirqus.Numbers;

namespace d60.Cirqus.Events
{
    public class Event
    {
        readonly DomainEvent _innerDomainEvent;
        readonly Metadata _innerMetadata;
        readonly byte[] _data;

        protected Event(DomainEvent domainEvent, byte[] data, Metadata meta)
        {
            _data = data;
            _innerDomainEvent = domainEvent;
            _innerMetadata = meta;
        }

        public static Event FromDomainEvent(DomainEvent domainEvent, byte[] data)
        {
            return new Event(domainEvent, data, null);
        }

        public DomainEvent DomainEvent
        {
            get
            {
                if (_innerDomainEvent == null)
                {
                    throw new InvalidOperationException("Can't get inner domain event from this event because it contains only binary event data");
                }
                return _innerDomainEvent;
            }
        }

        public static Event FromMetadata(Metadata meta, byte[] data)
        {
            return new Event(null, data, meta);
        }

        public Metadata Meta { get { return _innerMetadata ?? _innerDomainEvent.Meta; } }

        public virtual byte[] Data { get { return _data; } }

        public override string ToString()
        {
            return string.Format("Event[{0}] (chk: {1})", Data.Length, Data.Aggregate(255, (acc, b) => acc ^ b));
        }

        public bool IsSameAs(Event otherEvent)
        {
            var otherMeta = otherEvent.Meta;
            var otherData = otherEvent.Data;

            var meta = Meta;
            var data = Data;

            if (otherMeta.Count != meta.Count) return false;

            if (data.Length != otherData.Length) return false;

            foreach (var kvp in meta)
            {
                if (!otherMeta.ContainsKey(kvp.Key))
                    return false;

                if (otherMeta[kvp.Key] != kvp.Value)
                    return false;
            }

            for (var index = 0; index < data.Length; index++)
            {
                if (data[index] != otherData[index])
                    return false;
            }

            return true;
        }

        public static Event Clone(Event other)
        {
            return new Event(other._innerDomainEvent, other.Data, other._innerMetadata);
        }
    }
}
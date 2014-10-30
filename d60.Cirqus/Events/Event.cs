using System.Linq;
using d60.Cirqus.Numbers;

namespace d60.Cirqus.Events
{
    public class Event
    {
        public Event()
        {
            Meta = new Metadata();
            Data = new byte[0];
        }

        public Metadata Meta { get; set; }
        
        public virtual byte[] Data { get; set; }

        public override string ToString()
        {
            return string.Format("Event[{0}] (chk: {1})", Data.Length, Data.Aggregate(255, (acc, b) => acc ^ b));
        }

        public bool IsSameAs(Event otherEvent)
        {
            var otherMeta = otherEvent.Meta;
            var otherData = otherEvent.Data;

            if (otherMeta.Count != Meta.Count) return false;
            if (Data.Length != otherData.Length) return false;

            foreach (var kvp in Meta)
            {
                if (!otherMeta.ContainsKey(kvp.Key))
                    return false;

                if (otherMeta[kvp.Key] != kvp.Value)
                    return false;
            }

            for (var index = 0; index < Data.Length; index++)
            {
                if (Data[index] != otherData[index])
                    return false;
            }

            return true;
        }
    }
}
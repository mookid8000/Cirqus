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
    }
}
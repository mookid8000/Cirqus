using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using d60.Cirqus.Events;
using Newtonsoft.Json;

namespace d60.Cirqus.Testing
{
    public class EventCollection : IEnumerable<DomainEvent>
    {
        readonly IEnumerable<DomainEvent> _eventStream;

        internal EventCollection(IEnumerable<DomainEvent> eventStream)
        {
            _eventStream = eventStream;
        }

        public void WriteTo(TextWriter writer)
        {
            var events = _eventStream.ToList();

            foreach (var e in events)
            {
                writer.WriteLine(JsonConvert.SerializeObject(e, new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.Auto,
                    Formatting = Formatting.Indented
                }));
            }
        }

        public IEnumerator<DomainEvent> GetEnumerator()
        {
            return _eventStream.ToList().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
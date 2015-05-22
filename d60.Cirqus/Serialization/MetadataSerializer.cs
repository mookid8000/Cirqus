using d60.Cirqus.Events;
using d60.Cirqus.Numbers;
using Newtonsoft.Json;

namespace d60.Cirqus.Serialization
{
    /// <summary>
    /// Serializer for the metadata of <see cref="DomainEvent"/>
    /// </summary>
    public class MetadataSerializer
    {
        /// <summary>
        /// Serializes the metadata
        /// </summary>
        public string Serialize(Metadata metadata)
        {
            return JsonConvert.SerializeObject(metadata);
        }

        /// <summary>
        /// Deserializes the metadata
        /// </summary>
        public Metadata Deserialize(string text)
        {
            return JsonConvert.DeserializeObject<Metadata>(text);
        }
    }
}
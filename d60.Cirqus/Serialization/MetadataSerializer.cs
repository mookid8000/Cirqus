using d60.Cirqus.Numbers;
using Newtonsoft.Json;

namespace d60.Cirqus.Serialization
{
    public class MetadataSerializer
    {
        public string Serialize(Metadata metadata)
        {
            return JsonConvert.SerializeObject(metadata);
        }

        public Metadata Deserialize(string text)
        {
            return JsonConvert.DeserializeObject<Metadata>(text);
        }
    }
}
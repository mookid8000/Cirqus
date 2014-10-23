using System.Runtime.Serialization;

namespace d60.Cirqus.AzureServiceBus.Relay
{
    [DataContract]
    public class TransportMessage
    {
        [DataMember]
        public string Events { get; set; } 
    }
}
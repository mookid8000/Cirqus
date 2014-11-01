using System.Collections.Generic;
using System.Runtime.Serialization;

namespace d60.Cirqus.AzureServiceBus.Relay
{
    [DataContract]
    public class TransportMessage
    {
        [DataMember]
        public List<TransportEvent> Events { get; set; } 
    }

    [DataContract]
    public class TransportEvent
    {
        [DataMember]
        public byte[] Data { get; set; }

        [DataMember]
        public byte[] Meta { get; set; }        
    }
}
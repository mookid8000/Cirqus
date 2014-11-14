using System.Collections.Generic;
using MongoDB.Bson;

namespace d60.Cirqus.MongoDb.Events
{
    class MongoEvent
    {
        public Dictionary<string, string> Meta { get; set; }
        public byte[] Bin { get; set; }
        public BsonValue Body { get; set; }
        
        public long GlobalSequenceNumber { get; set; }
        public long SequenceNumber { get; set; }
        public string AggregateRootId { get; set; }
    }
}
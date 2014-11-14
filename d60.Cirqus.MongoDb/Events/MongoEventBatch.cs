using System.Collections.Generic;
using MongoDB.Bson.Serialization.Attributes;

namespace d60.Cirqus.MongoDb.Events
{
    class MongoEventBatch
    {
        [BsonId]
        public string BatchId { get; set; }

        public List<MongoEvent> Events { get; set; }
    }
}
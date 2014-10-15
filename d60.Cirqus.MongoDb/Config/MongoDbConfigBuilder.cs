using System.Collections.Generic;
using d60.Cirqus.MongoDb.Events;

namespace d60.Cirqus.MongoDb.Config
{
    public class MongoDbConfigBuilder
    {
        public MongoDbConfigBuilder()
        {
            SerializationMutators = new List<IJsonEventMutator>();
            DeserializationMutators = new List<IJsonEventMutator>();
        }

        public MongoDbConfigBuilder WithSerializationMutators(IEnumerable<IJsonEventMutator> mutators)
        {
            SerializationMutators.AddRange(mutators);
            return this;
        }

        public MongoDbConfigBuilder WithDeserializationMutators(IEnumerable<IJsonEventMutator> mutators)
        {
            DeserializationMutators.AddRange(mutators);
            return this;
        }

        internal List<IJsonEventMutator> SerializationMutators { get; private set; }

        internal List<IJsonEventMutator> DeserializationMutators { get; private set; }
    }
}
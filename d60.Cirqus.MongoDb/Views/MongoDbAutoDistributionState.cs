using System;
using System.Collections.Generic;
using System.Linq;
using d60.Cirqus.Views;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;

namespace d60.Cirqus.MongoDb.Views
{
    /// <summary>
    /// Implementation of <see cref="IAutoDistributionState"/> that uses MongoDB to communicate heartbeats and current state
    /// </summary>
    public class MongoDbAutoDistributionState : IAutoDistributionState
    {
        const string DocumentId = "__current_global_state__";
        readonly MongoDatabase _mongoDatabase;
        readonly string _collectionName;

        public MongoDbAutoDistributionState(MongoDatabase mongoDatabase, string collectionName)
        {
            _mongoDatabase = mongoDatabase;
            _collectionName = collectionName;
        }

        public IEnumerable<string> Heartbeat(string id, bool online)
        {
            var mongoCollection = _mongoDatabase.GetCollection<BsonDocument>(_collectionName);
            var heartbeatTime = online ? DateTime.UtcNow : DateTime.MinValue;

            var query = Query.EQ("_id", DocumentId);
            var update = Update.Set(string.Format("Heartbeats.{0}", id), BsonValue.Create(heartbeatTime));

            try
            {
                mongoCollection.Update(query, update, UpdateFlags.Upsert);

                if (!online) return new string[0];

                var currentState = GetCurrentState().FirstOrDefault(s => s.ManagerId == id);

                return currentState != null ? currentState.ViewIds : new HashSet<string>();
            }
            catch (MongoDuplicateKeyException)
            {
                if (!online) return new string[0];

                var currentState = GetCurrentState().FirstOrDefault(s => s.ManagerId == id);

                return currentState != null ? currentState.ViewIds : new HashSet<string>();
            }
        }

        public IEnumerable<AutoDistributionState> GetCurrentState()
        {
            var mongoCollection = _mongoDatabase.GetCollection<State>(_collectionName);

            var state = mongoCollection.FindOneById(DocumentId);

            if (state == null)
            {
                return Enumerable.Empty<AutoDistributionState>();
            }

            return state.Heartbeats
                .Where(kvp => (DateTime.UtcNow - kvp.Value) < TimeSpan.FromSeconds(5))
                .Select(kvp => new AutoDistributionState(kvp.Key, state.GetViewsFor(kvp.Key)))
                .ToList();
        }

        public void SetNewState(IEnumerable<AutoDistributionState> newState)
        {
            var newStateDictionary = newState.ToDictionary(s => s.ManagerId, s => s.ViewIds);
            var doc = BsonValue.Create(newStateDictionary);

            var mongoCollection = _mongoDatabase.GetCollection<BsonDocument>(_collectionName);
            var query = Query.EQ("_id", DocumentId);
            var update = Update.Set("Views", doc);

            mongoCollection.Update(query, update);
        }

        class State
        {
            public State()
            {
                Id = DocumentId;
                Heartbeats = new Dictionary<string, DateTime>();
                Views = new Dictionary<string, HashSet<string>>();
            }

            public string Id { get; private set; }

            public Dictionary<string, DateTime> Heartbeats { get; private set; }

            public Dictionary<string, HashSet<string>> Views { get; private set; }

            public HashSet<string> GetViewsFor(string key)
            {
                return Views.ContainsKey(key) ? Views[key] : new HashSet<string>();
            }
        }
    }
}
using System;
using d60.Cirqus.Logging;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace d60.Cirqus.MongoDb.Logging
{
    public class MongoDbLoggerFactory : CirqusLoggerFactory
    {
        readonly MongoCollection<LogStatement> _logStatements;

        public MongoDbLoggerFactory(MongoDatabase database, string collectionName)
        {
            _logStatements = database.GetCollection<LogStatement>(collectionName);
        }

        public override Logger GetLogger(Type ownerType)
        {
            return new MongoDbLogger(_logStatements, ownerType);
        }

        class MongoDbLogger : Logger
        {
            readonly MongoCollection<LogStatement> _logStatements;
            readonly Type _ownerType;

            public MongoDbLogger(MongoCollection<LogStatement> logStatements, Type ownerType)
            {
                _logStatements = logStatements;
                _ownerType = ownerType;
            }

            public override void Debug(string message, params object[] objs)
            {
                Write(Level.Debug, SafeFormat(message, objs));
            }

            public override void Info(string message, params object[] objs)
            {
                Write(Level.Info, SafeFormat(message, objs));
            }

            public override void Warn(string message, params object[] objs)
            {
                Write(Level.Warn, SafeFormat(message, objs));
            }

            public override void Error(string message, params object[] objs)
            {
                Write(Level.Error, SafeFormat(message, objs));
            }

            void Write(Level level, string text)
            {
                try
                {
                    _logStatements.Insert(new LogStatement
                    {
                        Level = level.ToString(),
                        Text = text,
                        Time = DateTime.Now,
                        OwnerType = _ownerType.FullName
                    }, WriteConcern.Unacknowledged);
                }
                catch { }
            }

            string SafeFormat(string message, object[] objs)
            {
                try
                {
                    return string.Format(message, objs);
                }
                catch
                {
                    return message;
                }
            }
        }

        class LogStatement
        {
            public LogStatement()
            {
                Id = ObjectId.GenerateNewId();
            }

            public ObjectId Id { get; private set; }

            [BsonElement("level")]
            public string Level { get; set; }

            [BsonElement("time")]
            public DateTime Time { get; set; }

            [BsonElement("owner")]
            public string OwnerType { get; set; }

            [BsonElement("text")]
            public string Text { get; set; }
        }
    }
}
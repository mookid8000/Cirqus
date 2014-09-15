using System;
using d60.Cirqus.Logging;
using MongoDB.Driver;

namespace d60.Cirqus.MongoDb.Logging
{
    public class MongoDbLoggerFactory : CirqusLoggerFactory
    {
        readonly MongoCollection _logStatements;

        public MongoDbLoggerFactory(MongoDatabase database, string collectionName)
        {
            _logStatements = database.GetCollection(collectionName);
        }

        public override Logger GetLogger(Type ownerType)
        {
            return new MongoDbLogger(_logStatements, ownerType);
        }

        class MongoDbLogger : Logger
        {
            readonly MongoCollection _logStatements;
            readonly Type _ownerType;

            public MongoDbLogger(MongoCollection logStatements, Type ownerType)
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

            public override void Warn(Exception exception, string message, params object[] objs)
            {
                Write(Level.Warn, SafeFormat(message, objs), exception);
            }

            public override void Error(string message, params object[] objs)
            {
                Write(Level.Error, SafeFormat(message, objs));
            }

            public override void Error(Exception exception, string message, params object[] objs)
            {
                Write(Level.Error, SafeFormat(message, objs), exception);
            }

            void Write(Level level, string text, Exception exception = null)
            {
                try
                {
                    if (exception == null)
                    {
                        _logStatements.Insert(new
                        {
                            level = level.ToString(),
                            text = text,
                            time = DateTime.Now,
                            owner = _ownerType.FullName
                        }, WriteConcern.Unacknowledged);
                    }
                    else
                    {
                        _logStatements.Insert(new
                        {
                            level = level.ToString(),
                            text = text,
                            time = DateTime.Now,
                            owner = _ownerType.FullName,
                            exception = exception.ToString()
                        }, WriteConcern.Unacknowledged);
                    }
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
    }
}
using System;
using System.Linq;
using d60.Cirqus.MongoDb.Config;
using MongoDB.Driver;
using NUnit.Framework;

namespace d60.Cirqus.Tests.MongoDb
{
    public class TestMongoDbLoggerFactory : FixtureBase
    {
        MongoDatabase _database;
        ICommandProcessor _commandProcessor;

        protected override void DoSetUp()
        {
            _database = MongoHelper.InitializeTestDatabase();

            _commandProcessor = CommandProcessor.With()
                .Logging(l => l.UseMongoDb(_database, "lost"))
                .EventStore(e => e.UseMongoDb(_database, "events"))
                .Create();

            RegisterForDisposal(_commandProcessor);
        }

        [Test]
        public void DoStuff()
        {
            var logStatements = _database.GetCollection("logs").FindAll().ToList();

            Console.WriteLine("---------------------------------------------------------------------------------------");
            Console.WriteLine(string.Join(Environment.NewLine, logStatements.Select(s => s["text"])));
            Console.WriteLine("---------------------------------------------------------------------------------------");
        }
    }
}
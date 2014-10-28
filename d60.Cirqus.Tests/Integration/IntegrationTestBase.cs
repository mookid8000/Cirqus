using System;
using d60.Cirqus.Aggregates;
using d60.Cirqus.Events;
using d60.Cirqus.MongoDb.Events;
using d60.Cirqus.MsSql.Events;
using d60.Cirqus.PostgreSql;
using d60.Cirqus.Serialization;
using d60.Cirqus.Testing.Internals;
using d60.Cirqus.Tests.MongoDb;
using d60.Cirqus.Tests.MsSql;
using d60.Cirqus.Tests.PostgreSql;
using d60.Cirqus.Tests.Stubs;
using d60.Cirqus.Views;
using MongoDB.Driver;

namespace d60.Cirqus.Tests.Integration
{
    public abstract class IntegrationTestBase : FixtureBase
    {
        MongoDatabase _mongoDatabase;

        protected override void DoSetUp()
        {
            
        }

        protected ICommandProcessor GetCommandProcessor(EventStoreOption eventStoreOption)
        {
            var eventStore = GetEventStore(eventStoreOption);

            var commandProcessor = new CommandProcessor(eventStore, new DefaultAggregateRootRepository(eventStore), new ConsoleOutEventDispatcher(),
                new DomainEventSerializer());

            RegisterForDisposal(commandProcessor);

            return commandProcessor;
        }

        IEventStore GetEventStore(EventStoreOption eventStoreOption)
        {
            switch (eventStoreOption)
            {
                case EventStoreOption.InMemory:
                    return new InMemoryEventStore();

                case EventStoreOption.MongoDb:
                    return new MongoDbEventStore(GetMongoDb(), "Events");

                case EventStoreOption.SqlServer:
                    MsSqlTestHelper.EnsureTestDatabaseExists();
                    MsSqlTestHelper.DropTable("Events");
                    return new MsSqlEventStore(MsSqlTestHelper.ConnectionString, "Events");

                case EventStoreOption.Postgres:
                    PostgreSqlTestHelper.DropTable("Events");
                    return new PostgreSqlEventStore(PostgreSqlTestHelper.PostgreSqlConnectionString, "Events");

                default:
                    throw new ArgumentOutOfRangeException("eventStoreOption", "Unknown event store option");
            }
        }

        MongoDatabase GetMongoDb()
        {
            if (_mongoDatabase != null)
            {
                return _mongoDatabase;
            }
            _mongoDatabase = MongoHelper.InitializeTestDatabase();
            return _mongoDatabase;
        }

        public enum EventStoreOption
        {
            InMemory,
            MongoDb,
            SqlServer,
            Postgres
        }
    }
}
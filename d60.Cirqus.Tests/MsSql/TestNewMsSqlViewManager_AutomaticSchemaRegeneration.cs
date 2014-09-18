using System;
using System.Data.SqlClient;
using d60.Cirqus.Events;
using d60.Cirqus.MsSql.Views;
using d60.Cirqus.Tests.Stubs;
using d60.Cirqus.Views.ViewManagers;
using d60.Cirqus.Views.ViewManagers.Locators;
using NUnit.Framework;

namespace d60.Cirqus.Tests.MsSql
{
    [TestFixture]
    public class TestNewMsSqlViewManager_AutomaticSchemaRegeneration : FixtureBase
    {
        MsSqlViewManager<SomeView> _viewManager;
        string _connectionString;

        protected override void DoSetUp()
        {
            MsSqlTestHelper.EnsureTestDatabaseExists();

            _connectionString = MsSqlTestHelper.ConnectionString;

            MsSqlTestHelper.DropTable("views");
        }

        [Test]
        public void AutomaticallyRegeneratesSchemaIfColumnIsMissing()
        {
            // arrange
            _viewManager = new MsSqlViewManager<SomeView>(_connectionString, "views", automaticallyCreateSchema: true);

            // schema is generated now - let's drop a column and re-initialize the view manager
            DropColumn("views", "ColumnA");

            // act
            _viewManager = new MsSqlViewManager<SomeView>(_connectionString, "views", automaticallyCreateSchema: true);

            // assert
            _viewManager.Dispatch(new ThrowingViewContext(), new[]
            {
                GetAnEvent(Guid.NewGuid(), 0),
                GetAnEvent(Guid.NewGuid(), 1)
            });

            var view = _viewManager.Load(GlobalInstanceLocator.GetViewInstanceId());
            Assert.That(view.Events, Is.EqualTo(2));
        }

        void DropColumn(string tableName, string columnName)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();

                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = string.Format("alter table [{0}] drop column [{1}];", tableName, columnName);

                    cmd.ExecuteNonQuery();
                }
            }
        }

        public class SomeView : IViewInstance<GlobalInstanceLocator>, ISubscribeTo<AnEvent>
        {
            public string Id { get; set; }
            public long LastGlobalSequenceNumber { get; set; }

            public string ColumnA { get; set; }

            public string ColumnB { get; set; }

            public int Events { get; set; }

            public void Handle(IViewContext context, AnEvent domainEvent)
            {
                Events++;
            }
        }

        public class AnEvent : DomainEvent
        {
        }

        static AnEvent GetAnEvent(Guid aggregateRootId, int globalSequenceNumber)
        {
            return new AnEvent
            {
                Meta =
                {
                    {DomainEvent.MetadataKeys.AggregateRootId, aggregateRootId},
                    {DomainEvent.MetadataKeys.SequenceNumber, 0},
                    {DomainEvent.MetadataKeys.GlobalSequenceNumber, globalSequenceNumber},
                }
            };
        }
    }
}
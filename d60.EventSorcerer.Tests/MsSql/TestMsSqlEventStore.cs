using System;
using System.Collections.Generic;
using d60.EventSorcerer.Events;
using d60.EventSorcerer.MsSql;
using d60.EventSorcerer.MsSql.Views;
using d60.EventSorcerer.TestHelpers;
using d60.EventSorcerer.Views.Basic;
using d60.EventSorcerer.Views.Basic.Locators;
using NUnit.Framework;

namespace d60.EventSorcerer.Tests.MsSql
{
    [TestFixture]
    public class TestMsSqlEventStore : FixtureBase
    {
        MsSqlCatchUpViewManager<ViewWithManyPropertyTypes> _viewManager;

        protected override void DoSetUp()
        {
            TestSqlHelper.EnsureTestDatabaseExists();

            var connectionString = SqlHelper.GetConnectionString(TestSqlHelper.ConnectionStringName);

            TestSqlHelper.DropTable(connectionString, "views");

            _viewManager = new MsSqlCatchUpViewManager<ViewWithManyPropertyTypes>(TestSqlHelper.ConnectionStringName, "views");
        }

        [Test]
        public void VerifyDataTypes()
        {
            var aggregateRootId = Guid.NewGuid();

            _viewManager.Dispatch(new InMemoryEventStore(), new[] { GetAnEvent(aggregateRootId) });

            var view = _viewManager.Load(InstancePerAggregateRootLocator.GetViewIdFromGuid(aggregateRootId));
            
            Assert.That(view, Is.Not.Null, "View was not properly generated");
            Assert.That(view.String, Is.EqualTo("a string"));
        }

        static AnEvent GetAnEvent(Guid aggregateRootId)
        {
            return new AnEvent
            {
                Meta =
                {
                    {DomainEvent.MetadataKeys.AggregateRootId, aggregateRootId},
                    {DomainEvent.MetadataKeys.SequenceNumber, 0},
                    {DomainEvent.MetadataKeys.GlobalSequenceNumber, 0},
                }
            };
        }

        class ViewWithManyPropertyTypes : IView<InstancePerAggregateRootLocator>, ISubscribeTo<AnEvent>
        {
            public ViewWithManyPropertyTypes()
            {
                String = "a string";

                Integer = 2;
                Shorty = 3;
                Long = 4;

                Double = 2.3;
                Decimal = 2.4m;
                Float = 1.2f;

                ListOfString = new List<string> { "hello", "there" };
                ListOfInt = new List<int> { 6, 7 };
            }
            public string Id { get; set; }

            // string
            public string String { get; set; }

            // int types
            public int Integer { get; set; }
            public short Shorty { get; set; }
            public long Long { get; set; }

            // float types
            public double Double { get; set; }
            public decimal Decimal { get; set; }
            public float Float { get; set; }

            // special treatment
            public List<string> ListOfString { get; set; }
            public List<int> ListOfInt { get; set; }

            public void Handle(AnEvent domainEvent)
            {

            }
        }
    }

    public class AnEvent : DomainEvent
    {
    }
}
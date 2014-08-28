using System;
using System.Collections.Generic;
using d60.Cirqus.Events;
using d60.Cirqus.MsSql.Views;
using d60.Cirqus.TestHelpers.Internals;
using d60.Cirqus.Tests.Stubs;
using d60.Cirqus.Views.ViewManagers;
using d60.Cirqus.Views.ViewManagers.Locators;
using NUnit.Framework;

namespace d60.Cirqus.Tests.MsSql
{
    [TestFixture]
    public class TestMsSqlEventStore : FixtureBase
    {
        MsSqlViewManager<ViewInstanceWithManyPropertyTypes> _viewManager;

        protected override void DoSetUp()
        {
            MsSqlTestHelper.EnsureTestDatabaseExists();

            var connectionString = MsSqlTestHelper.ConnectionString;

            MsSqlTestHelper.DropTable("views");

            _viewManager = new MsSqlViewManager<ViewInstanceWithManyPropertyTypes>(connectionString, "views");
            _viewManager.Initialize(new ThrowingViewContext(), new InMemoryEventStore());
        }

        [Test]
        public void VerifyDataTypes()
        {
            var aggregateRootId = Guid.NewGuid();

            _viewManager.Dispatch(new ThrowingViewContext(), new InMemoryEventStore(), new[] { GetAnEvent(aggregateRootId) });

            var view = _viewManager.Load(InstancePerAggregateRootLocator.GetViewIdFromGuid(aggregateRootId));

            Assert.That(view, Is.Not.Null, "View was not properly generated");
            Assert.That(view.String, Is.EqualTo("a string"));
            Assert.That(view.Integer, Is.EqualTo(2));
            Assert.That(view.Shorty, Is.EqualTo(3));
            Assert.That(view.Long, Is.EqualTo(4));
            Assert.That(view.Double, Is.EqualTo(2.3));
            Assert.That(view.Decimal, Is.EqualTo(2.4m));
            Assert.That(view.Float, Is.EqualTo(1.2f));
            Assert.That(view.ListOfString, Is.EqualTo(new List<string> { "hello", "there" }));
            Assert.That(view.ListOfInt, Is.EqualTo(new List<int> { 6, 7 }));
            Assert.That(view.ListOfDouble, Is.EqualTo(new List<double> { 6, 7 }));
            Assert.That(view.ListOfDecimal, Is.EqualTo(new List<decimal> { 6, 7 }));
            Assert.That(view.HashOfStrings, Is.EqualTo(new HashSet<string> { "bim", "bom" }));
            Assert.That(view.HashOfInts, Is.EqualTo(new HashSet<int> { 9, 3 }));
            Assert.That(view.ArrayOfStrings, Is.EqualTo(new[] { "hej", "med", "dig", "min", "ven" }));
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

        class ViewInstanceWithManyPropertyTypes : IViewInstance<InstancePerAggregateRootLocator>, ISubscribeTo<AnEvent>
        {
            public ViewInstanceWithManyPropertyTypes()
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
                ListOfDouble = new List<double> { 6, 7 };
                ListOfDecimal = new List<decimal> { 6, 7 };

                HashOfStrings = new HashSet<string> { "bim", "bom" };
                HashOfInts = new HashSet<int> { 9, 3 };
                ArrayOfStrings = new[] { "hej", "med", "dig", "min", "ven" };
            }
            public string Id { get; set; }
            public long LastGlobalSequenceNumber { get; set; }

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
            public List<double> ListOfDouble { get; set; }
            public List<decimal> ListOfDecimal { get; set; }
            public HashSet<string> HashOfStrings { get; set; }
            public HashSet<int> HashOfInts { get; set; }
            public string[] ArrayOfStrings { get; set; }

            public void Handle(IViewContext context, AnEvent domainEvent)
            {

            }
        }
    }

    public class AnEvent : DomainEvent
    {
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using d60.Cirqus.Events;
using d60.Cirqus.MsSql.Views;
using d60.Cirqus.Tests.Stubs;
using d60.Cirqus.Views.ViewManagers;
using d60.Cirqus.Views.ViewManagers.Locators;
using NUnit.Framework;

namespace d60.Cirqus.Tests.MsSql
{
    [TestFixture]
    public class TestNewMsSqlViewManager : FixtureBase
    {
        MsSqlViewManager<ViewInstanceWithManyPropertyTypes> _viewManager;

        protected override void DoSetUp()
        {
            MsSqlTestHelper.EnsureTestDatabaseExists();

            var connectionString = MsSqlTestHelper.ConnectionString;

            MsSqlTestHelper.DropTable("views");

            _viewManager = new MsSqlViewManager<ViewInstanceWithManyPropertyTypes>(connectionString, "views");
        }

        [Test]
        public void VerifyDataTypes()
        {
            var aggregateRootId = Guid.NewGuid();

            _viewManager.Dispatch(new ThrowingViewContext(), new DomainEvent[] { GetAnEvent(aggregateRootId) });

            var view = _viewManager.Load(InstancePerAggregateRootLocator.GetViewIdFromAggregateRootId(aggregateRootId));

            Assert.That(view, Is.Not.Null, "View was not properly generated");
            Assert.That(view.NullString, Is.Null);
            Assert.That(view.NullInt, Is.Null);
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
            
            Assert.That(view.DateTime, Is.EqualTo(new DateTime(1979, 3, 19, 13, 00, 00, DateTimeKind.Utc)));
            Assert.That(view.DateTimeOffset, Is.EqualTo(new DateTimeOffset(1979, 3, 19, 14, 00, 00, TimeSpan.FromHours(1))));
            Assert.That(view.TimeSpan, Is.EqualTo(new TimeSpan(2, 15, 20)));

            Assert.That(string.Join(" ", view.JustSomethingComplex.Children.Select(c => c.Message)), Is.EqualTo("oh my god"));
            Assert.That(string.Join(" ", view.ListOfComplexThings.SelectMany(t => t.Children.Select(c => c.Message))), Is.EqualTo("oh my god woota da f00k"));
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

        class SomethingComplex
        {
            public List<SomethingElse> Children { get; set; }
        }

        class SomethingElse
        {
            public string Message { get; set; }
        }

        class ViewInstanceWithManyPropertyTypes : IViewInstance<InstancePerAggregateRootLocator>, ISubscribeTo<AnEvent>
        {
            public ViewInstanceWithManyPropertyTypes()
            {
                // this bad boy is intentionally set to null
                NullString = null;
                NullInt = null;

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

                DateTime = new DateTime(1979, 3, 19, 14, 00, 00, DateTimeKind.Local);
                DateTimeOffset = new DateTimeOffset(1979, 3, 19, 14, 00, 00, TimeSpan.FromHours(1));
                TimeSpan = new TimeSpan(2, 15, 20);

                JustSomethingComplex = new SomethingComplex
                {
                    Children = new List<SomethingElse>
                    {
                        new SomethingElse {Message = "oh"},
                        new SomethingElse {Message = "my"},
                        new SomethingElse {Message = "god"},
                    }
                };

                ListOfComplexThings = new List<SomethingComplex>
                {
                    new SomethingComplex
                    {
                        Children = new List<SomethingElse>
                        {
                            new SomethingElse {Message = "oh"},
                            new SomethingElse {Message = "my"},
                            new SomethingElse {Message = "god"},
                        }
                    },
                    new SomethingComplex
                    {
                        Children = new List<SomethingElse>
                        {
                            new SomethingElse {Message = "woota"},
                            new SomethingElse {Message = "da"},
                            new SomethingElse {Message = "f00k"},
                        }
                    }
                };
            }

            public string Id { get; set; }
            public long LastGlobalSequenceNumber { get; set; }

            public string NullString { get; set; }
            public int? NullInt { get; set; }

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
            public DateTime DateTime { get; set; }
            public DateTimeOffset DateTimeOffset { get; set; }
            public TimeSpan TimeSpan { get; set; }

            [Json]
            public SomethingComplex JustSomethingComplex { get; set; }

            [Json]
            public List<SomethingComplex> ListOfComplexThings { get; set; }

            public void Handle(IViewContext context, AnEvent domainEvent)
            {

            }
        }

        public class AnEvent : DomainEvent { }
    }
}
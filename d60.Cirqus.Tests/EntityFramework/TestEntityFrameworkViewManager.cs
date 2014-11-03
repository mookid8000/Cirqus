using System;
using System.Collections.Generic;
using System.Linq;
using d60.Cirqus.Aggregates;
using d60.Cirqus.EntityFramework;
using d60.Cirqus.Events;
using d60.Cirqus.Logging;
using d60.Cirqus.Logging.Console;
using d60.Cirqus.Tests.MsSql;
using d60.Cirqus.Views.ViewManagers;
using d60.Cirqus.Views.ViewManagers.Locators;
using NUnit.Framework;
using TestContext = d60.Cirqus.Testing.TestContext;

namespace d60.Cirqus.Tests.EntityFramework
{
    [TestFixture]
    public class TestEntityFrameworkViewManager : FixtureBase
    {
        EntityFrameworkViewManager<SomeParent> _viewManager;
        TestContext _context;

        protected override void DoSetUp()
        {
            CirqusLoggerFactory.Current = new ConsoleLoggerFactory(minLevel: Logger.Level.Info);

            MsSqlTestHelper.EnsureTestDatabaseExists();
            MsSqlTestHelper.DropTable("__MigrationHistory");
            MsSqlTestHelper.DropTable("SomeParent_Position");
            MsSqlTestHelper.DropTable("AnotherChilds");
            MsSqlTestHelper.DropTable("SomeChilds");
            MsSqlTestHelper.DropTable("SomeParent");

            _viewManager = new EntityFrameworkViewManager<SomeParent>(MsSqlTestHelper.ConnectionString);

            _context = new TestContext()
                .AddViewManager(_viewManager);

            RegisterForDisposal(_context);
        }

        [Test, Description(@"This test was created when child objects were observed to be left in the DB *with their FKs intact* - i.e. they would not be disassociated with their parents as they should. The problem turned out to be due to a missing 'virtual' on the collection property")]
        public void DoesNotLeaveConnectedOrphans()
        {
            var ids = Enumerable.Range(0, 10).Select(i => i.ToString());

            foreach (var id in ids)
            {
                RunTest(id);
            }

            //RunTestFor(new Guid("9506BDAC-65A6-4793-90A0-F46C7F725461"));
            //RunTestFor(new Guid("E758C4B9-D65C-4E0F-BA28-0FE468CBBE59"));
            //RunTestFor(new Guid("1798AA40-8E7C-4169-AABE-3EB8134F7428"));
            //RunTestFor(new Guid("8266A9C8-773A-4F8D-9568-12E54427B424"));
            //RunTestFor(new Guid("23B79D7F-1B48-4B2A-8197-59DC4267DF48"));
        }

        [Test]
        public void ThrowsWhenMappingEntityWithNonVirtualCollectionProperty()
        {
            var exception = Assert
                .Throws<InvalidOperationException>(() => new EntityFrameworkViewManager<ParentWithNonVirtualCollectionProperty>(MsSqlTestHelper.ConnectionString));

            Assert.That(exception.Message, Contains.Substring("must be declared virtual"));
        }

        class ParentWithNonVirtualCollectionProperty : IViewInstance<GlobalInstanceLocator>, ISubscribeTo
        {
            public string Id { get; set; }
            public long LastGlobalSequenceNumber { get; set; }
            public List<SomeChild> Children { get; set; }
        }

        void RunTest(string id)
        {
            _context.Save(id, new Event { NumberOfChildren = 1 });
            _context.Save(id, new Event { NumberOfChildren = 2 });
            _context.Save(id, new Event { NumberOfChildren = 3 });
            _context.Save(id, new Event { NumberOfChildren = 2 });
            _context.Save(id, new Event { NumberOfChildren = 2 });
            _context.Save(id, new Event { NumberOfChildren = 3 });
            _context.Save(id, new Event { NumberOfChildren = 2 });
            _context.Save(id, new Event { NumberOfChildren = 2 });
            _context.Save(id, new Event { NumberOfChildren = 3 });
            _context.Save(id, new Event { NumberOfChildren = 2 });

            using (var context = _viewManager.CreateContext())
            {
                var viewId = InstancePerAggregateRootLocator.GetViewIdFromAggregateRootId(id);

                var parent = context.Views
                    .First(v => v.Id == viewId);

                Assert.That(parent.Children.Count, Is.EqualTo(2));
                Assert.That(parent.OtherChildren.Count, Is.EqualTo(4));
            }
        }

        public class Root : AggregateRoot { }

        public class Event : DomainEvent<Root>
        {
            public int NumberOfChildren { get; set; }
        }

        public class SomeParent : IViewInstance<InstancePerAggregateRootLocator>, ISubscribeTo<Event>
        {
            public SomeParent()
            {
                Children = new List<SomeChild>();
                OtherChildren = new List<AnotherChild>();
            }

            public virtual string Id { get; set; }

            public virtual long LastGlobalSequenceNumber { get; set; }

            public virtual List<SomeChild> Children { get; set; }

            public virtual List<AnotherChild> OtherChildren { get; set; }

            public virtual void Handle(IViewContext context, Event domainEvent)
            {
                Children.Clear();

                OtherChildren.Clear();

                Children.AddRange(Enumerable.Range(0, domainEvent.NumberOfChildren)
                    .Select(no => new SomeChild { Value = "child " + no }));

                OtherChildren.AddRange(Enumerable.Range(0, domainEvent.NumberOfChildren*2)
                    .Select(no => new AnotherChild {Value = "other child " + no}));
            }
        }

        public class SomeChild
        {
            public virtual int Id { get; set; }
            public virtual string Value { get; set; }
        }

        public class AnotherChild
        {
            public virtual int Id { get; set; }
            public virtual string Value { get; set; }
        }
    }
}
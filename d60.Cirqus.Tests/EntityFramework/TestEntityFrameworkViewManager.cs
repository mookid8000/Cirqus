using System;
using System.Collections.Generic;
using System.Linq;
using d60.Cirqus.Aggregates;
using d60.Cirqus.EntityFramework;
using d60.Cirqus.Events;
using d60.Cirqus.Tests.MsSql;
using d60.Cirqus.Views.ViewManagers;
using d60.Cirqus.Views.ViewManagers.Locators;
using FluentNHibernate.Conventions;
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
            MsSqlTestHelper.EnsureTestDatabaseExists();
            MsSqlTestHelper.DropTable("__MigrationHistory");
            MsSqlTestHelper.DropTable("SomeParent_Position");
            MsSqlTestHelper.DropTable("SomeChilds");
            MsSqlTestHelper.DropTable("SomeParent");

            _viewManager = new EntityFrameworkViewManager<SomeParent>(MsSqlTestHelper.ConnectionString);

            _context = new TestContext()
                .AddViewManager(_viewManager);
        }

        [Test]
        public void StatementOfSomething()
        {
            var id = new Guid("9506BDAC-65A6-4793-90A0-F46C7F725461");

            _context.Save(id, new Event { NumberOfChildren = 1 });
            _context.Save(id, new Event { NumberOfChildren = 2 });
            _context.Save(id, new Event { NumberOfChildren = 3 });
            _context.Save(id, new Event { NumberOfChildren = 2 });

            using (var context = _viewManager.CreateContext())
            {
                var parent = context.Views
                    .First(v => v.Id == InstancePerAggregateRootLocator.GetViewIdFromAggregateRootId(id));

                Assert.That(parent.Children.Count, Is.EqualTo(2));
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
            }

            public virtual string Id { get; set; }

            public virtual long LastGlobalSequenceNumber { get; set; }

            public virtual List<SomeChild> Children { get; set; }

            public virtual void Handle(IViewContext context, Event domainEvent)
            {
                Children.Clear();

                Children.AddRange(Enumerable.Range(0, domainEvent.NumberOfChildren)
                    .Select(no => new SomeChild { Value = "child " + no }));
            }
        }

        public class SomeChild
        {
            public virtual int Id { get; set; }
            public virtual string Value { get; set; }
        }
    }
}
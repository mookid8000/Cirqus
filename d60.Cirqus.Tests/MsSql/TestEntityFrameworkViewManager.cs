using System;
using System.Linq;
using d60.Cirqus.Aggregates;
using d60.Cirqus.EntityFramework;
using d60.Cirqus.Events;
using d60.Cirqus.Views.ViewManagers;
using d60.Cirqus.Views.ViewManagers.Locators;
using NUnit.Framework;
using TestContext = d60.Cirqus.TestHelpers.TestContext;

namespace d60.Cirqus.Tests.MsSql
{
    [TestFixture, Category(TestCategories.MsSql)]
    public class TestEntityFrameworkViewManager : FixtureBase
    {
        TestContext _context;
        EntityFrameworkViewManager<ViewInstance> _viewManager;

        protected override void DoSetUp()
        {
            MsSqlTestHelper.DropTable("__MigrationHistory");
            MsSqlTestHelper.DropTable("View");

            _viewManager = new EntityFrameworkViewManager<ViewInstance>(MsSqlTestHelper.ConnectionString);
            _context = new TestContext().AddViewManager(_viewManager);
        }

        [Test]
        public void CanDoTheFunkyLinqyStuff()
        {
            // arrange
            var root1Id = Guid.NewGuid();
            var root2Id = Guid.NewGuid();
            var root3Id = Guid.NewGuid();
            var root4Id = Guid.NewGuid();
            var root5Id = Guid.NewGuid();
            using (var uow = _context.BeginUnitOfWork())
            {
                uow.Get<Root>(root1Id).ThisIsTheNumber(1);
                uow.Get<Root>(root2Id).ThisIsTheNumber(2);
                uow.Get<Root>(root3Id).ThisIsTheNumber(43);
                uow.Get<Root>(root4Id).ThisIsTheNumber(43);
                uow.Get<Root>(root5Id).ThisIsTheNumber(43);
                uow.Commit();
            }

            // act
            // assert
            using (var views = _viewManager.Linq())
            {
                Assert.That(views.Query().First(v => v.Id == root1Id.ToString()).SomeNumber, Is.EqualTo(1));                
                Assert.That(views.Query().First(v => v.Id == root2Id.ToString()).SomeNumber, Is.EqualTo(2));                
                
                Assert.That(views.Query().Count(v => v.SomeNumber == 43), Is.EqualTo(3));                
            }

        }

        public class Root : AggregateRoot, IEmit<Event>
        {
            public void ThisIsTheNumber(int number)
            {
                Emit(new Event { SomeNumber = number });
            }
            public void Apply(Event e)
            {
            }
        }

        public class Event : DomainEvent<Root>
        {
            public int SomeNumber { get; set; }
        }

        class ViewInstance : IViewInstance<InstancePerAggregateRootLocator>, ISubscribeTo<Event>
        {
            public string Id { get; set; }
            public long LastGlobalSequenceNumber { get; set; }
            public int SomeNumber { get; set; }
            public void Handle(IViewContext context, Event domainEvent)
            {
                SomeNumber = domainEvent.SomeNumber;
            }
        }
    }
}
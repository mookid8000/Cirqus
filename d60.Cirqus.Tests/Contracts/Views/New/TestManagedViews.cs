using System;
using d60.Cirqus.Events;
using d60.Cirqus.Tests.Contracts.Views.New.Factories;
using d60.Cirqus.Tests.Contracts.Views.New.Models;
using d60.Cirqus.Views.ViewManagers.Locators;
using NUnit.Framework;
using TestContext = d60.Cirqus.TestHelpers.TestContext;

namespace d60.Cirqus.Tests.Contracts.Views.New
{
    [TestFixture(typeof(MongoDbManagedViewFactory), Category = TestCategories.MongoDb)]
    [TestFixture(typeof(MsSqlManagedViewFactory), Category = TestCategories.MsSql)]
    public class TestManagedViews<TFactory> : FixtureBase where TFactory : ManagedViewFactoryBase, new()
    {
        TFactory _factory;
        TestContext _context;

        protected override void DoSetUp()
        {
            _factory = new TFactory();

            _context = new TestContext();
        }

        [Test]
        public void WorksWithSimpleScenario()
        {
            // arrange
            var view = _factory.CreateManagedView<GeneratedIds>();
            _context.AddViewManager(view);

            // act
            _context.ProcessCommand(new GenerateNewId(IdGenerator.InstanceId) { IdBase = "bim" });
            _context.ProcessCommand(new GenerateNewId(IdGenerator.InstanceId) { IdBase = "bim" });
            var last = _context.ProcessCommand(new GenerateNewId(IdGenerator.InstanceId) { IdBase = "bom" });

            // assert
            view.WaitUntilDispatched(last).Wait();

            var idsView = view.Load(InstancePerAggregateRootLocator.GetViewIdFromAggregateRootId(IdGenerator.InstanceId));
            
            Assert.That(idsView.AllIds.Count, Is.EqualTo(3));
         
            Assert.That(idsView.AllIds, Contains.Item("bim/0"));
            Assert.That(idsView.AllIds, Contains.Item("bim/1"));
            Assert.That(idsView.AllIds, Contains.Item("bom/0"));
        }

        [Test]
        public void AutomaticallyCatchesUpWhenInitializing()
        {
            // arrange
            _context.ProcessCommand(new GenerateNewId(IdGenerator.InstanceId) { IdBase = "bim" });
            _context.ProcessCommand(new GenerateNewId(IdGenerator.InstanceId) { IdBase = "bim" });
            var last = _context.ProcessCommand(new GenerateNewId(IdGenerator.InstanceId) { IdBase = "bom" });

            // act
            var view = _factory.CreateManagedView<GeneratedIds>();
            _context.AddViewManager(view);

            // assert
            view.WaitUntilDispatched(last).Wait();

            var idsView = view.Load(InstancePerAggregateRootLocator.GetViewIdFromAggregateRootId(IdGenerator.InstanceId));

            Assert.That(idsView.AllIds.Count, Is.EqualTo(3));

            Assert.That(idsView.AllIds, Contains.Item("bim/0"));
            Assert.That(idsView.AllIds, Contains.Item("bim/1"));
            Assert.That(idsView.AllIds, Contains.Item("bom/0"));
        }

        [Test]
        public void AutomaticallyCatchesUpAfterPurging()
        {
            // arrange
            var view = _factory.CreateManagedView<GeneratedIds>();
            _context.AddViewManager(view);

            _context.ProcessCommand(new GenerateNewId(IdGenerator.InstanceId) { IdBase = "bim" });
            _context.ProcessCommand(new GenerateNewId(IdGenerator.InstanceId) { IdBase = "bim" });
            var last = _context.ProcessCommand(new GenerateNewId(IdGenerator.InstanceId) { IdBase = "bom" });

            // act
            _factory.PurgeView<GeneratedIds>();

            // assert
            view.WaitUntilDispatched(last).Wait();

            var idsView = view.Load(InstancePerAggregateRootLocator.GetViewIdFromAggregateRootId(IdGenerator.InstanceId));

            Assert.That(idsView.AllIds.Count, Is.EqualTo(3));

            Assert.That(idsView.AllIds, Contains.Item("bim/0"));
            Assert.That(idsView.AllIds, Contains.Item("bim/1"));
            Assert.That(idsView.AllIds, Contains.Item("bom/0"));
        }

        [Test]
        public void CanManageViewWithLocatorWithMultipleIds()
        {
            // arrange
            var view = _factory.CreateManagedView<HeaderCounter>();
            _context.AddViewManager(view);

            // act
            _context.ProcessCommand(new EmitEvent(Guid.NewGuid()));
            _context.ProcessCommand(new EmitEvent(Guid.NewGuid()));
            _context.ProcessCommand(new EmitEvent(Guid.NewGuid()));
            var last = _context.ProcessCommand(new EmitEvent(Guid.NewGuid()));

            // assert
            view.WaitUntilDispatched(last).Wait();

            var batchIdView = view.Load(DomainEvent.MetadataKeys.BatchId);
            var aggregateRootIdView = view.Load(DomainEvent.MetadataKeys.AggregateRootId);
            var globalSequenceNumberView = view.Load(DomainEvent.MetadataKeys.GlobalSequenceNumber);
            var sequenceNumberView = view.Load(DomainEvent.MetadataKeys.SequenceNumber);

            Assert.That(batchIdView.HeaderValues.Count, Is.EqualTo(4));
            Assert.That(aggregateRootIdView.HeaderValues.Count, Is.EqualTo(4));
            Assert.That(globalSequenceNumberView.HeaderValues.Count, Is.EqualTo(4));
            Assert.That(sequenceNumberView.HeaderValues.Count, Is.EqualTo(1));
        }
    }
}
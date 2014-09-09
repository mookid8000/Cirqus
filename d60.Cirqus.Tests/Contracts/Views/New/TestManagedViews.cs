using System;
using System.Linq;
using d60.Cirqus.Events;
using d60.Cirqus.Logging;
using d60.Cirqus.Logging.Console;
using d60.Cirqus.Tests.Contracts.Views.New.Factories;
using d60.Cirqus.Tests.Contracts.Views.New.Models;
using d60.Cirqus.Views.ViewManagers.Locators;
using NUnit.Framework;
using TestContext = d60.Cirqus.TestHelpers.TestContext;

namespace d60.Cirqus.Tests.Contracts.Views.New
{
    [TestFixture(typeof(MongoDbManagedViewFactory), Category = TestCategories.MongoDb)]
    [TestFixture(typeof(MsSqlManagedViewFactory), Category = TestCategories.MsSql)]
    public class TestManagedViews<TFactory> : FixtureBase where TFactory : AbstractManagedViewFactory, new()
    {
        TFactory _factory;
        TestContext _context;

        protected override void DoSetUp()
        {
            CirqusLoggerFactory.Current = new ConsoleLoggerFactory(minLevel:Logger.Level.Debug);

            _factory = new TFactory();

            _context = new TestContext();
            RegisterForDisposal(_context);
        }

        [Test]
        public void WorksWithSimpleScenario()
        {
            // arrange
            Console.WriteLine("Adding view manager for GeneratedIds");
            var view = _factory.GetManagedView<GeneratedIds>();
            _context.AddViewManager(view);

            // act
            Console.WriteLine("Processing 2 commands");
            _context.ProcessCommand(new GenerateNewId(IdGenerator.InstanceId) { IdBase = "bim" });
            _context.ProcessCommand(new GenerateNewId(IdGenerator.InstanceId) { IdBase = "bim" });
            var last = _context.ProcessCommand(new GenerateNewId(IdGenerator.InstanceId) { IdBase = "bom" });

            // assert
            Console.WriteLine("Waiting until dispatched: {0}", last.GlobalSequenceNumbersOfEmittedEvents.Max());
            view.WaitUntilProcessed(last, TimeSpan.FromSeconds(2)).Wait();

            var idsView = view.Load(InstancePerAggregateRootLocator.GetViewIdFromAggregateRootId(IdGenerator.InstanceId));

            Assert.That(idsView, Is.Not.Null, "Could not find view!");
            Assert.That(idsView.AllIds.Count, Is.EqualTo(3));
         
            Assert.That(idsView.AllIds, Contains.Item("bim/0"));
            Assert.That(idsView.AllIds, Contains.Item("bim/1"));
            Assert.That(idsView.AllIds, Contains.Item("bom/0"));
        }

        [Test]
        public void AutomaticallyCatchesUpWhenInitializing()
        {
            // arrange
            Console.WriteLine("Processing 2 commands");
            _context.ProcessCommand(new GenerateNewId(IdGenerator.InstanceId) { IdBase = "bim" });
            _context.ProcessCommand(new GenerateNewId(IdGenerator.InstanceId) { IdBase = "bim" });
            var last = _context.ProcessCommand(new GenerateNewId(IdGenerator.InstanceId) { IdBase = "bom" });

            // act
            Console.WriteLine("Adding view manager for GeneratedIds");
            var view = _factory.GetManagedView<GeneratedIds>();
            _context.AddViewManager(view);

            // assert
            Console.WriteLine("Waiting until dispatched: {0}", last.GlobalSequenceNumbersOfEmittedEvents.Max());
            view.WaitUntilProcessed(last, TimeSpan.FromSeconds(2)).Wait();

            var idsView = view.Load(InstancePerAggregateRootLocator.GetViewIdFromAggregateRootId(IdGenerator.InstanceId));

            Assert.That(idsView, Is.Not.Null, "Could not find view!");
            Assert.That(idsView.AllIds.Count, Is.EqualTo(3));

            Assert.That(idsView.AllIds, Contains.Item("bim/0"));
            Assert.That(idsView.AllIds, Contains.Item("bim/1"));
            Assert.That(idsView.AllIds, Contains.Item("bom/0"));
        }

        [Test]
        public void AutomaticallyCatchesUpAfterPurging()
        {
            // arrange
            Console.WriteLine("Adding view manager for GeneratedIds");
            var view = _factory.GetManagedView<GeneratedIds>();
            _context.AddViewManager(view);

            Console.WriteLine("Processing 2 commands");
            _context.ProcessCommand(new GenerateNewId(IdGenerator.InstanceId) { IdBase = "bim" });
            _context.ProcessCommand(new GenerateNewId(IdGenerator.InstanceId) { IdBase = "bim" });
            var last = _context.ProcessCommand(new GenerateNewId(IdGenerator.InstanceId) { IdBase = "bom" });

            // act
            Console.WriteLine("Purging view");
            _factory.PurgeView<GeneratedIds>();

            // assert
            view.WaitUntilProcessed(last, TimeSpan.FromSeconds(2)).Wait();

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
            const string customHeaderKey = "custom-header";
            var view = _factory.GetManagedView<HeaderCounter>();
            _context.AddViewManager(view);

            // act
            _context.ProcessCommand(new EmitEvent(Guid.NewGuid()) {Meta = {{customHeaderKey, "w00t!"}}});
            _context.ProcessCommand(new EmitEvent(Guid.NewGuid()));
            _context.ProcessCommand(new EmitEvent(Guid.NewGuid()));
            var last = _context.ProcessCommand(new EmitEvent(Guid.NewGuid()));

            // assert
            view.WaitUntilProcessed(last, TimeSpan.FromSeconds(2)).Wait();

            var batchIdView = view.Load(DomainEvent.MetadataKeys.BatchId);
            var aggregateRootIdView = view.Load(DomainEvent.MetadataKeys.AggregateRootId);
            var globalSequenceNumberView = view.Load(DomainEvent.MetadataKeys.GlobalSequenceNumber);
            var sequenceNumberView = view.Load(DomainEvent.MetadataKeys.SequenceNumber);
            var customHeaderView = view.Load(customHeaderKey);

            Assert.That(batchIdView.HeaderValues.Count, Is.EqualTo(4));
            Assert.That(aggregateRootIdView.HeaderValues.Count, Is.EqualTo(4));
            Assert.That(globalSequenceNumberView.HeaderValues.Count, Is.EqualTo(4));
            Assert.That(sequenceNumberView.HeaderValues.Count, Is.EqualTo(1));
            Assert.That(customHeaderView.HeaderValues.Count, Is.EqualTo(1));
        }
    }
}
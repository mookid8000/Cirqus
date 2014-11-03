using System;
using d60.Cirqus.Events;
using d60.Cirqus.Logging;
using d60.Cirqus.Logging.Console;
using d60.Cirqus.Tests.Contracts.Views.Factories;
using d60.Cirqus.Tests.Contracts.Views.Models.GeneralViewManagerTest;
using d60.Cirqus.Views.ViewManagers;
using d60.Cirqus.Views.ViewManagers.Locators;
using NUnit.Framework;
using TestContext = d60.Cirqus.Testing.TestContext;

namespace d60.Cirqus.Tests.Contracts.Views
{
    [TestFixture(typeof(MongoDbViewManagerFactory), Category = TestCategories.MongoDb)]
    [TestFixture(typeof(MsSqlViewManagerFactory), Category = TestCategories.MsSql)]
    [TestFixture(typeof(EntityFrameworkViewManagerFactory), Category = TestCategories.MsSql, Ignore = true, IgnoreReason = "The contained HashSet<string> cannot be persisted by EF")]
    [TestFixture(typeof(InMemoryViewManagerFactory))]
    public class GeneralViewManagerTests<TFactory> : FixtureBase where TFactory : AbstractViewManagerFactory, new()
    {
        readonly TimeSpan _defaultTimeout = TimeSpan.FromSeconds(5);

        TFactory _factory;
        TestContext _context;

        protected override void DoSetUp()
        {
            CirqusLoggerFactory.Current = new ConsoleLoggerFactory(minLevel:Logger.Level.Debug);

            _factory = RegisterForDisposal(new TFactory());

            _context = RegisterForDisposal(new TestContext{Asynchronous = true});
        }

        [Test]
        public void WorksWithSimpleScenario()
        {
            // arrange
            Console.WriteLine("Adding view manager for GeneratedIds");
            var view = _factory.GetViewManager<GeneratedIds>();
            _context.AddViewManager(view);

            // act
            Console.WriteLine("Processing 2 commands");
            _context.ProcessCommand(new GenerateNewId(IdGenerator.InstanceId) { IdBase = "bim" });
            _context.ProcessCommand(new GenerateNewId(IdGenerator.InstanceId) { IdBase = "bim" });
            var last = _context.ProcessCommand(new GenerateNewId(IdGenerator.InstanceId) { IdBase = "bom" });

            // assert
            Console.WriteLine("Waiting until dispatched: {0}", last.GetNewPosition());
            view.WaitUntilProcessed(last, _defaultTimeout).Wait();

            var idsView = _factory.Load<GeneratedIds>(InstancePerAggregateRootLocator.GetViewIdFromAggregateRootId(IdGenerator.InstanceId));

            Assert.That(idsView, Is.Not.Null, "Could not find view!");
            
            var storedIds = idsView.AllIds;
            Assert.That(storedIds.Count, Is.EqualTo(3));
         
            Assert.That(storedIds, Contains.Item("bim/0"));
            Assert.That(storedIds, Contains.Item("bim/1"));
            Assert.That(storedIds, Contains.Item("bom/0"));
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
            var view = _factory.GetViewManager<GeneratedIds>();
            _context.AddViewManager(view);

            // assert
            Console.WriteLine("Waiting until dispatched: {0}", last.GetNewPosition());
            view.WaitUntilProcessed(last, _defaultTimeout).Wait();

            var idsView = _factory.Load<GeneratedIds>(InstancePerAggregateRootLocator.GetViewIdFromAggregateRootId(IdGenerator.InstanceId));

            Assert.That(idsView, Is.Not.Null, "Could not find view!");
            
            var storedIds = idsView.AllIds;
            Assert.That(storedIds.Count, Is.EqualTo(3));

            Assert.That(storedIds, Contains.Item("bim/0"));
            Assert.That(storedIds, Contains.Item("bim/1"));
            Assert.That(storedIds, Contains.Item("bom/0"));
        }

        [Test]
        public void AutomaticallyCatchesUpAfterPurging()
        {
            // arrange
            Console.WriteLine("Adding view manager for GeneratedIds");
            var view = _factory.GetViewManager<GeneratedIds>();
            _context.AddViewManager(view);

            Console.WriteLine("Processing 2 commands");
            _context.ProcessCommand(new GenerateNewId(IdGenerator.InstanceId) { IdBase = "bim" });
            _context.ProcessCommand(new GenerateNewId(IdGenerator.InstanceId) { IdBase = "bim" });
            var last = _context.ProcessCommand(new GenerateNewId(IdGenerator.InstanceId) { IdBase = "bom" });

            view.WaitUntilProcessed(last, _defaultTimeout).Wait();

            // act
            Console.WriteLine("Purging view");
            _factory.PurgeView<GeneratedIds>();

            // assert
            view.WaitUntilProcessed(last, _defaultTimeout).Wait();

            var idsView = _factory
                .Load<GeneratedIds>(InstancePerAggregateRootLocator.GetViewIdFromAggregateRootId(IdGenerator.InstanceId));

            var storedIds = idsView.AllIds;

            Assert.That(storedIds.Count, Is.EqualTo(3));

            Assert.That(storedIds, Contains.Item("bim/0"));
            Assert.That(storedIds, Contains.Item("bim/1"));
            Assert.That(storedIds, Contains.Item("bom/0"));
        }

        [Test]
        public void CanManageViewWithLocatorWithMultipleIds()
        {
            // arrange
            const string customHeaderKey = "custom-header";
            var view = _factory.GetViewManager<HeaderCounter>();
            _context.AddViewManager(view);

            // act
            _context.ProcessCommand(new EmitEvent("id1") {Meta = {{customHeaderKey, "w00t!"}}});
            _context.ProcessCommand(new EmitEvent("id2"));
            _context.ProcessCommand(new EmitEvent("id3"));
            var last = _context.ProcessCommand(new EmitEvent("id4"));

            // assert
            view.WaitUntilProcessed(last, _defaultTimeout).Wait();

            var batchIdView = LoadCheckForNull(view, DomainEvent.MetadataKeys.BatchId);
            var aggregateRootIdView = LoadCheckForNull(view, DomainEvent.MetadataKeys.AggregateRootId);
            var globalSequenceNumberView = LoadCheckForNull(view, DomainEvent.MetadataKeys.GlobalSequenceNumber);
            var sequenceNumberView = LoadCheckForNull(view, DomainEvent.MetadataKeys.SequenceNumber);
            var customHeaderView = LoadCheckForNull(view, customHeaderKey);

            Assert.That(batchIdView.HeaderValues.Count, Is.EqualTo(4));
            Assert.That(aggregateRootIdView.HeaderValues.Count, Is.EqualTo(4));
            Assert.That(globalSequenceNumberView.HeaderValues.Count, Is.EqualTo(4));
            Assert.That(sequenceNumberView.HeaderValues.Count, Is.EqualTo(1));
            Assert.That(customHeaderView.HeaderValues.Count, Is.EqualTo(1));
        }

        static HeaderCounter LoadCheckForNull(IViewManager<HeaderCounter> view, string metadataKey)
        {
            var loadedView = view.Load(metadataKey);

            if (loadedView == null)
            {
                throw new AssertionException(string.Format("Could not find view with ID '{0}'", metadataKey));
            }

            return loadedView;
        }
    }
}
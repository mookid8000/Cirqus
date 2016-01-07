using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using d60.Cirqus.Commands;
using d60.Cirqus.Logging;
using d60.Cirqus.Logging.Console;
using d60.Cirqus.MongoDb.Views;
using d60.Cirqus.Tests.Extensions;
using d60.Cirqus.Tests.MongoDb.Model;
using MongoDB.Driver;
using NUnit.Framework;

namespace d60.Cirqus.Tests.MongoDb
{
    [TestFixture]
    public class TestViewInstancesFailingIndividually : FixtureBase
    {
        ICommandProcessor _commandProcessor;
        MongoDatabase _database;
        MongoDbViewManager<ViewThatCanFail> _viewManager;

        protected override void DoSetUp()
        {
            CirqusLoggerFactory.Current = new ConsoleLoggerFactory(Logger.Level.Debug);

            _database = MongoHelper.InitializeTestDatabase();

            _viewManager = new MongoDbViewManager<ViewThatCanFail>(_database);

            _commandProcessor = CommandProcessor.With()
                .EventStore(e => e.UseInMemoryEventStore())
                .EventDispatcher(e => e.UseViewManagerEventDispatcher(_viewManager)
                    .WithMaxDomainEventsPerBatch(1))
                .Create();

            RegisterForDisposal(_commandProcessor);
        }

        [Test]
        public async Task ViewInstanceGetsMarkedAsFailed()
        {
            var commands = new List<Command>
            {
                new IncrementYourself("id1"),
                new IncrementYourself("id1"),
                new IncrementYourself("id1"),
                new IncrementYourself("id1"),
                new IncrementYourself("id1"),

                new IncrementYourself("id2"),
                new IncrementYourself("id2"),
                new IncrementYourself("id2"),
                new IncrementYourself("id2"),
                new IncrementYourself("id2"),

                new IncrementYourself("id3"),
                new IncrementYourself("id3"),
                new IncrementYourself("id3"),
                new IncrementYourself("id3"),
                new IncrementYourself("id3"),
            };

            commands.ForEach(c => _commandProcessor.ProcessCommand(c));

            // wait until these two individual bad boys have been updated
            // - can't use CommandProcessingResult though, as each view
            // instance will be updated individually
            await WaitUntilViewInstanceHasProcessed(4, "id1");
            await WaitUntilViewInstanceHasProcessed(9, "id2");

            var failedInstance = _viewManager.Load("id3");
            Assert.That(failedInstance.Failed, Is.True);
            Assert.That(failedInstance.Number, Is.EqualTo(3), "Expected the view to have been updated to three because that's where it failed");
        }

        async Task WaitUntilViewInstanceHasProcessed(long globalSequenceNumberToWaitFor, string id1)
        {
            var stopwatch = Stopwatch.StartNew();

            while (true)
            {
                var viewInstancePosition = _viewManager.Load(id1)?.LastGlobalSequenceNumber ?? -1;

                if (viewInstancePosition >= globalSequenceNumberToWaitFor) return;

                await Task.Delay(100);

                if (stopwatch.Elapsed > TimeSpan.FromSeconds(5))
                {
                    throw new AssertionException($"View instance {id1} did not catch up to {globalSequenceNumberToWaitFor} within 5 s timeout!");
                }
            }
        }
    }
}
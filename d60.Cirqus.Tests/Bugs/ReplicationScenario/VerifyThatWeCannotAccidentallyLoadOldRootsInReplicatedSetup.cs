using System;
using System.Collections.Generic;
using System.Threading;
using d60.Cirqus.Aggregates;
using d60.Cirqus.Config;
using d60.Cirqus.Events;
using d60.Cirqus.Logging;
using d60.Cirqus.Logging.Console;
using d60.Cirqus.MongoDb.Events;
using d60.Cirqus.MongoDb.Views;
using d60.Cirqus.Serialization;
using d60.Cirqus.Tests.MongoDb;
using d60.Cirqus.Views;
using NUnit.Framework;

namespace d60.Cirqus.Tests.Bugs.ReplicationScenario
{
    [TestFixture]
    [Description(@"Verifies a pretty finicky scenario where WE THOUGHT THAT delayed replication could accidentally lead to loading an old aggregate root.

CHILL OUT! THIS WAS NEVER THE CASE, BECAUSE THE EVENT DISPATCHER USES THE RESOLVED AGGREGATE ROOT REPOSITORY TO LOAD AGGREGATE ROOTS,
WHICH ENSURES THAT IT GETS THE COMMAND PROCESSING EVENT STORE WHEN IT IS TIME TO LOAD SOMETHING.

You can stop reading here if you want :)

You can continue reading if you are curious about the details of the scenario we were scared of (but which turned out to never have been possible).

Let's pretend that we have a system that uses replication to increase responsiveness in one of the locations.

We could use the two locations 'Aalborg' and 'Hong Kong' as examples.

The Central Source Of Truth Event Store happens to reside in Aalborg, and then the system replicates events to Hong Kong, where views are processed.

This introduces a risk of the following nasty sequence of events:

* Command is created and processed in Hong Kong...
* ...which is fulfilled by loading an aggregate root hydrated by Aalborg events.
* The aggregate root emits some new events...
* ...which naturally get saved to the Aalborg event store.
* All is fine and dandy at this point.... except!!:
* The emitted events are residing in-mem after having saved the batch...
* ...in Hong Kong - so the newly emitted events naturally get dispatched to the event dispatcher...
* ...which attempts to deliver the events to its views...
* ...which is done successfully, because they happen to fit just right with that the views are expecting next.

All this is fine! There is a risk, though, that a Hong Kong view chooses to use the view context's ability to LOAD AN AGGREGATE ROOT, e.g.
the instance that just emitted an event.... and this aggregate root hydration will BE BASED ON EVENTS FROM THE HONG KONG EVENT STORE, which
is not guaranteed to have caught up with Aalborg at this point, which will cause the loading of an outdated aggregate root!

This must not happen. This test is to verify that it does not happen.

In order to trigger this situation, we introduce a forced delay between saving the event batch and their replication.

And we will repeat again here, at the bottom: There was never any problem. We were just afraid that there could have been a problem. Better safe than sorry ;)
")]
    public class VerifyThatWeCannotAccidentallyLoadOldRootsInReplicatedSetup : FixtureBase
    {
        ICommandProcessor _hongKongCommandProcessor;
        MongoDbViewManager<CountingRootView> _hongKongViewManager;

        protected override void DoSetUp()
        {
            CirqusLoggerFactory.Current = new ConsoleLoggerFactory(Logger.Level.Warn);

            var mongoDatabase = MongoHelper.InitializeTestDatabase();

            var aalborgEventStore = new MongoDbEventStore(mongoDatabase, "AalborgEvents");
            var hongKongEventStore = new MongoDbEventStore(mongoDatabase, "HongKongEvents");

            CreateCommandProcessor(aalborgEventStore, aalborgEventStore);

            _hongKongViewManager = new MongoDbViewManager<CountingRootView>(mongoDatabase);
            _hongKongCommandProcessor = CreateCommandProcessor(aalborgEventStore, hongKongEventStore, _hongKongViewManager);

            var replicationDelay = TimeSpan.FromSeconds(5);

            CreateAndStartReplication(aalborgEventStore, hongKongEventStore, replicationDelay);
        }

        [Test]
        public void CheckIt()
        {
            Console.WriteLine("Processing command");
            var result = _hongKongCommandProcessor.ProcessCommand(new IncrementCountingRoot("root1"));

            Console.WriteLine("Waiting for view to process event");
            _hongKongViewManager
                .WaitUntilProcessed(result, TimeSpan.FromSeconds(20))
                .Wait();

            Console.WriteLine("Loading view instance");
            var instance = _hongKongViewManager.Load("root1");

            Console.WriteLine("Checking it");
            Assert.That(instance.Number, Is.EqualTo(1));
        }

        ICommandProcessor CreateCommandProcessor(IEventStore commandProcessingEventStore, IEventStore eventProcessingEventStore, params IViewManager[] viewManagers)
        {
            var commandProcessor = CommandProcessor.With()
                .EventStore(e => e.Register(c => commandProcessingEventStore))
                .EventDispatcher(e => e.Register(c =>
                {
                    var aggregateRootRepository = c.Get<IAggregateRootRepository>();
                    var domainEventSerializer = c.Get<IDomainEventSerializer>();
                    var domainTypeNameMapper = c.Get<IDomainTypeNameMapper>();
                    return new ViewManagerEventDispatcher(
                        aggregateRootRepository,
                        eventProcessingEventStore,
                        domainEventSerializer,
                        domainTypeNameMapper,
                        viewManagers);
                }))
                .Create();

            RegisterForDisposal(commandProcessor);

            return commandProcessor;
        }

        EventReplicator CreateAndStartReplication(IEventStore aalborgEventStore, IEventStore hongKongEventStore, TimeSpan replicationDelay)
        {
            var replicator = new EventReplicator(aalborgEventStore, new EventReplicatorDelayer(hongKongEventStore, replicationDelay));
            RegisterForDisposal(replicator);
            replicator.Start();
            return replicator;
        }

        class EventReplicatorDelayer : IEventStore
        {
            readonly IEventStore _innerEventStore;
            readonly TimeSpan _replicationDelay;

            public EventReplicatorDelayer(IEventStore innerEventStore, TimeSpan replicationDelay)
            {
                _innerEventStore = innerEventStore;
                _replicationDelay = replicationDelay;
            }

            public void Save(Guid batchId, IEnumerable<EventData> batch)
            {
                Console.WriteLine("Replicating event ... waiting");
                Thread.Sleep(_replicationDelay);

                _innerEventStore.Save(batchId, batch);
                Console.WriteLine("....and BAM! it was replicated!");
            }

            public IEnumerable<EventData> Load(string aggregateRootId, long firstSeq = 0)
            {
                return _innerEventStore.Load(aggregateRootId, firstSeq);
            }

            public IEnumerable<EventData> Stream(long globalSequenceNumber = 0)
            {
                return _innerEventStore.Stream(globalSequenceNumber);
            }

            public long GetNextGlobalSequenceNumber()
            {
                return _innerEventStore.GetNextGlobalSequenceNumber();
            }
        }
    }
}
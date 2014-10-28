using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using d60.Cirqus.Aggregates;
using d60.Cirqus.Commands;
using d60.Cirqus.Config;
using d60.Cirqus.Diagnostics;
using d60.Cirqus.Events;
using d60.Cirqus.Logging;
using d60.Cirqus.MongoDb.Config;
using d60.Cirqus.MongoDb.Events;
using d60.Cirqus.Serialization;
using d60.Cirqus.Tests.MongoDb;
using d60.Cirqus.Tests.Stubs;
using NUnit.Framework;
using Timer = System.Timers.Timer;

namespace d60.Cirqus.Tests.Diagnostics
{
    [TestFixture, Category(TestCategories.MongoDb)]
    public class TestLapTimes : FixtureBase
    {
        [TestCase(1000)]
        public void CanDoTheThing(int numberOfCommandsToProcess)
        {
            var database = MongoHelper.InitializeTestDatabase();
            var profiler = new MyProfiler();

            using (var timer = new Timer(1000))
            {
                var commandCounter = 0;

                timer.Elapsed += (o, ea) =>
                {
                    var numberOfCommands = Interlocked.Exchange(ref commandCounter, 0);
                    Console.WriteLine("{0} commands/s", numberOfCommands);
                };

                timer.Start();

                var commandProcessor = RegisterForDisposal(
                    CommandProcessor.With()
                        .Logging(l => l.UseConsole(minLevel: Logger.Level.Warn))
                        .EventStore(e => e.UseMongoDb(database, "Events"))
                      
                        // try commenting this line in/out
                        .AggregateRootRepository(e => e.EnableInMemorySnapshotCaching(1000))
                        
                        .EventDispatcher(e => e.UseViewManagerEventDispatcher())
                        .Options(o => o.AddProfiler(profiler))
                        .Create()
                    );

                var id = new Guid("67509467-C686-410C-8862-E910B5AF70F0");

                numberOfCommandsToProcess.Times(() =>
                {
                    commandProcessor.ProcessCommand(new MakeStuffHappen(id));
                    Interlocked.Increment(ref commandCounter);
                });

                var repo = new DefaultAggregateRootRepository(new MongoDbEventStore(database, "Events"), new DomainEventSerializer());
                var currentState = repo.Get<Root>(id, new ConsoleOutUnitOfWork(repo));

                Assert.That(currentState.AggregateRoot.HowManyThingsHaveHappened, Is.EqualTo(numberOfCommandsToProcess));
            }

            Console.WriteLine(profiler);
        }

        public class Root : AggregateRoot, IEmit<SomethingHappened>
        {
            int _howManyThingsHaveHappened;

            public int HowManyThingsHaveHappened
            {
                get { return _howManyThingsHaveHappened; }
            }

            public void MakeStuffHappen()
            {
                Emit(new SomethingHappened());
            }

            public void Apply(SomethingHappened e)
            {
                _howManyThingsHaveHappened++;
            }
        }

        public class SomethingHappened : DomainEvent<Root> { }

        public class MakeStuffHappen : Command<Root>
        {
            public MakeStuffHappen(Guid aggregateRootId)
                : base(aggregateRootId)
            {
            }

            public override void Execute(Root aggregateRoot)
            {
                aggregateRoot.MakeStuffHappen();
            }
        }
    }

    public class MyProfiler : IProfiler
    {
        readonly ConcurrentDictionary<Type, TimeSpan> _aggregateRootHydrationTimes = new ConcurrentDictionary<Type, TimeSpan>();
        
        long _millisecondsSpentSavingEvents = 0;
        long _millisecondsSpentGettingNextSequenceNumber = 0;

        public void RecordAggregateRootGet(TimeSpan elapsed, Type type, Guid aggregateRootId)
        {
            _aggregateRootHydrationTimes.AddOrUpdate(type, id => elapsed, (id, total) => total + elapsed);
        }

        public void RecordAggregateRootExists(TimeSpan elapsed, Guid aggregateRootId)
        {
        }

        public void RecordEventBatchSave(TimeSpan elapsed, Guid batchId)
        {
            Interlocked.Add(ref _millisecondsSpentSavingEvents, (long) elapsed.TotalMilliseconds);
        }

        public void RecordGlobalSequenceNumberGetNext(TimeSpan elapsed)
        {
            Interlocked.Add(ref _millisecondsSpentGettingNextSequenceNumber, (long)elapsed.TotalMilliseconds);
        }

        public void RecordEventDispatch(TimeSpan elapsed)
        {
            
        }

        public ConcurrentDictionary<Type, TimeSpan> AggregateRootHydrationTimes
        {
            get { return _aggregateRootHydrationTimes; }
        }

        public long MillisecondsSpentSavingEvents
        {
            get { return _millisecondsSpentSavingEvents; }
        }

        public long MillisecondsSpentGettingNextSequenceNumber
        {
            get { return _millisecondsSpentGettingNextSequenceNumber; }
        }

        public override string ToString()
        {
            return string.Format(@"Aggregate root hydration times
{0}

Time spent saving events: {1}

Time spent getting next sequence number: {2}",
                                             string.Join(Environment.NewLine, _aggregateRootHydrationTimes.Select(kvp => string.Format("    {0}: {1}", kvp.Key, kvp.Value))),
                                             TimeSpan.FromMilliseconds(_millisecondsSpentSavingEvents),
                                             TimeSpan.FromMilliseconds(_millisecondsSpentGettingNextSequenceNumber));
        }
    }
}
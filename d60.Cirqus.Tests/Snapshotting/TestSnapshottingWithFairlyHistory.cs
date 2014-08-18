using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using d60.Cirqus.Aggregates;
using d60.Cirqus.Commands;
using d60.Cirqus.Config;
using d60.Cirqus.Events;
using d60.Cirqus.Logging;
using d60.Cirqus.Logging.Console;
using d60.Cirqus.MongoDb.Events;
using d60.Cirqus.Snapshotting;
using d60.Cirqus.Tests.MongoDb;
using d60.Cirqus.Views.ViewManagers;
using MongoDB.Driver;
using NUnit.Framework;

namespace d60.Cirqus.Tests.Snapshotting
{
    [TestFixture, Category(TestCategories.MongoDb)]
    public class TestSnapshottingWithFairlyHistory : FixtureBase
    {
        MongoDatabase _database;
        TimeTaker _timeTaker;

        protected override void DoSetUp()
        {
            _database = MongoHelper.InitializeTestDatabase();

            CirqusLoggerFactory.Current = new ConsoleLoggerFactory(minLevel:Logger.Level.Warn);
        }

        /// <summary>
        /// Total time spent
        ///     hydrating roots: 28.1
        ///     loading events: 27.5
        ///     saving events: 7.7
        /// 
        /// 
        /// </summary>
        [TestCase(false, 100, 10000)]
        [TestCase(true, 100, 10000)]
        public void RunTest(bool useCaching, int numberOfRoots, int numberOfCommands)
        {
            var aggregateRootIds = Enumerable.Range(0, numberOfRoots).Select(i => Guid.NewGuid()).ToArray();
            var random = new Random(DateTime.Now.GetHashCode());
            Func<Guid> getRandomRootId = () => aggregateRootIds[random.Next(aggregateRootIds.Length)];

            var commandProcessor = GetCommandProcessor(useCaching);
            var processedCommands = 0L;

            TakeTime(string.Format("Processing {0} commands distributed among {1} roots", numberOfCommands, numberOfRoots),
                () => Enumerable.Range(0, numberOfCommands)
                    .Select(i => new CrushItRealGood(getRandomRootId(), 0.0001m))
                    .ToList()
                    .ForEach(cmd =>
                    {
                        commandProcessor.ProcessCommand(cmd);
                        Interlocked.Increment(ref processedCommands);
                    }),
                    timeSpan => Console.WriteLine("{0} commands processed.... ", Interlocked.Read(ref processedCommands)));

            Console.WriteLine(@"Total time spent
    hydrating roots: {0:0.0}
    loading events: {1:0.0}
    saving events: {2:0.0}

caching in use: {3}",
                _timeTaker.TimeSpentHydratingAggregateRoots.TotalSeconds,
                _timeTaker.TimeSpentLoadingEvents.TotalSeconds,
                _timeTaker.TimeSpentSavingEvents.TotalSeconds,
                useCaching);
        }

        CommandProcessor GetCommandProcessor(bool useCaching)
        {
            var eventStore = new MongoDbEventStore(_database, "events");

            _timeTaker = new TimeTaker
            {
                InnerEventStore = eventStore,
            };

            IAggregateRootRepository aggregateRootRepository = new DefaultAggregateRootRepository(_timeTaker);

            if (useCaching)
            {
                aggregateRootRepository = new CachingAggregateRootRepository(aggregateRootRepository, new InMemorySnapshotCache());
            }

            _timeTaker.InnerAggregateRootRepository = aggregateRootRepository;

            var commandProcessor = new CommandProcessor(_timeTaker, _timeTaker, new ViewManagerEventDispatcher(_timeTaker));
            return commandProcessor;
        }

        class TimeTaker : IAggregateRootRepository, IEventStore
        {
            TimeSpan _timeSpentHydratingAggregateRoots;
            TimeSpan _timeSpentLoadingEvents;
            TimeSpan _timeSpentSavingEvents;

            public IAggregateRootRepository InnerAggregateRootRepository { get; set; }

            public IEventStore InnerEventStore { get; set; }

            public TimeSpan TimeSpentHydratingAggregateRoots
            {
                get { return _timeSpentHydratingAggregateRoots; }
            }

            public TimeSpan TimeSpentLoadingEvents
            {
                get { return _timeSpentLoadingEvents; }
            }

            public TimeSpan TimeSpentSavingEvents
            {
                get { return _timeSpentSavingEvents; }
            }

            public AggregateRootInfo<TAggregate> Get<TAggregate>(Guid aggregateRootId, IUnitOfWork unitOfWork, long maxGlobalSequenceNumber = Int64.MaxValue) where TAggregate : AggregateRoot, new()
            {
                var stopwatch = Stopwatch.StartNew();
                
                var aggregateRootInfo = InnerAggregateRootRepository
                    .Get<TAggregate>(aggregateRootId, unitOfWork, maxGlobalSequenceNumber);
                
                _timeSpentHydratingAggregateRoots += stopwatch.Elapsed;

                return aggregateRootInfo;
            }

            public bool Exists<TAggregate>(Guid aggregateRootId, long maxGlobalSequenceNumber = Int64.MaxValue, IUnitOfWork unitOfWork = null) where TAggregate : AggregateRoot
            {
                return InnerAggregateRootRepository.Exists<TAggregate>(aggregateRootId, maxGlobalSequenceNumber, unitOfWork);
            }

            public void Save(Guid batchId, IEnumerable<DomainEvent> batch)
            {
                var stopwatch = Stopwatch.StartNew();

                InnerEventStore.Save(batchId, batch);

                _timeSpentSavingEvents += stopwatch.Elapsed;
            }

            public IEnumerable<DomainEvent> Load(Guid aggregateRootId, long firstSeq = 0, long limit = Int32.MaxValue)
            {
                var stopwatch = Stopwatch.StartNew();

                var domainEvents = InnerEventStore.Load(aggregateRootId, firstSeq, limit).ToList();

                _timeSpentLoadingEvents += stopwatch.Elapsed;

                return domainEvents;
            }

            public long GetNextSeqNo(Guid aggregateRootId)
            {
                return InnerEventStore.GetNextSeqNo(aggregateRootId);
            }

            public IEnumerable<DomainEvent> Stream(long globalSequenceNumber = 0)
            {
                return InnerEventStore.Stream(globalSequenceNumber);
            }
        }

        public class Beetroot : AggregateRoot, IEmit<BeetrootSquashed>, IEmit<BeetrootCrushed>
        {
            decimal _structuralStamina = 1;
            bool _completelyCrushed;

            public void Crush(decimal howMuch)
            {
                if (_completelyCrushed) return;

                Emit(new BeetrootSquashed(howMuch));

                if (_structuralStamina > 0) return;

                Emit(new BeetrootCrushed());
            }

            public void Apply(BeetrootSquashed e)
            {
                _structuralStamina -= e.HowMuch;
            }

            public void Apply(BeetrootCrushed e)
            {
                _completelyCrushed = true;
            }
        }

        public class BeetrootSquashed : DomainEvent<Beetroot>
        {
            public BeetrootSquashed(decimal howMuch)
            {
                HowMuch = howMuch;
            }

            public decimal HowMuch { get; private set; }
        }

        public class BeetrootCrushed : DomainEvent<Beetroot>
        {
        }

        public class CrushItRealGood : Command<Beetroot>
        {
            public CrushItRealGood(Guid aggregateRootId, decimal howMuch) : base(aggregateRootId)
            {
                HowMuch = howMuch;
            }

            public decimal HowMuch { get; private set; }

            public override void Execute(Beetroot aggregateRoot)
            {
                aggregateRoot.Crush(HowMuch);
            }
        }
    }
}
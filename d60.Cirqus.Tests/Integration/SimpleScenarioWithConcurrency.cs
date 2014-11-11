using System;
using System.Linq;
using System.Threading;
using d60.Cirqus.Aggregates;
using d60.Cirqus.Commands;
using d60.Cirqus.Events;
using d60.Cirqus.MongoDb.Events;
using d60.Cirqus.Serialization;
using d60.Cirqus.Tests.MongoDb;
using d60.Cirqus.Tests.Stubs;
using MongoDB.Driver;
using NUnit.Framework;

namespace d60.Cirqus.Tests.Integration
{
    [TestFixture]
    [Category(TestCategories.MongoDb)]
    [Description(@"Simulates the entire pipeline of event processing:

1. command comes in
2. command is mapped to an operation on an aggregate root
3. new unit of work is created
4. operation is invoked, events are collected
5. collected events are submitted atomically to event store
    a) on duplicate key error: go back to (3)
6. await synchronous views
7. publish events for consumption by chaser views

many time in parallel, and after some time the consistency of everything is verified
")]
    public class SimpleScenarioWithConcurrency : FixtureBase
    {
        DefaultAggregateRootRepository _aggregateRootRepository;
        CommandProcessor _cirqus;
        MongoDatabase _mongoDatabase;
        readonly JsonDomainEventSerializer _domainEventSerializer = new JsonDomainEventSerializer();
        readonly DefaultCommandMapper _commandMapper = new DefaultCommandMapper();

        protected override void DoSetUp()
        {
            _mongoDatabase = MongoHelper.InitializeTestDatabase();
            var eventStore = new MongoDbEventStore(_mongoDatabase, "events", automaticallyCreateIndexes: true);

            _aggregateRootRepository = new DefaultAggregateRootRepository(eventStore, _domainEventSerializer);

            var viewManager = new ConsoleOutEventDispatcher();

            _cirqus = new CommandProcessor(eventStore, _aggregateRootRepository, viewManager, _domainEventSerializer, _commandMapper);

            RegisterForDisposal(_cirqus);
        }

        [TestCase(1, 100, 10)]
        [TestCase(2, 100, 10)]
        [TestCase(3, 100, 10)]
        [TestCase(5, 100, 10)]
        [TestCase(1, 1000, 40, Ignore = TestCategories.IgnoreLongRunning)]
        [TestCase(2, 1000, 40, Ignore = TestCategories.IgnoreLongRunning)]
        [TestCase(3, 1000, 40, Ignore = TestCategories.IgnoreLongRunning)]
        [TestCase(5, 1000, 40, Ignore = TestCategories.IgnoreLongRunning)]
        [TestCase(1, 1000, 500, Ignore = TestCategories.IgnoreLongRunning)]
        [TestCase(2, 1000, 500, Ignore = TestCategories.IgnoreLongRunning)]
        [TestCase(3, 1000, 500, Ignore = TestCategories.IgnoreLongRunning)]
        [TestCase(5, 1000, 500, Ignore = TestCategories.IgnoreLongRunning)]
        public void RunEntirePipelineAndProbePrivatesForMultipleAggregates(int parallellism, int numberOfOperations, int numberOfAggregates)
        {
            var description = string.Format("{0} threads performing {1} ops distributed evenly among {2} aggregate roots", parallellism, numberOfOperations, numberOfAggregates);

            TakeTime(description, () => RunTest(parallellism, numberOfOperations, numberOfAggregates));
        }

        void RunTest(int parallellism, int numberOfOperations, int numberOfAggregates)
        {
            var random = new Random(DateTime.Now.GetHashCode());
            var aggregateRootIds = Enumerable.Range(0, numberOfAggregates).Select(i => i.ToString()).ToArray();

            Func<string> getRandomAggregateRootId = () => aggregateRootIds[random.Next(aggregateRootIds.Length)];

            var threads = Enumerable
                .Range(0, parallellism)
                .Select(i => new Thread(() =>
                {
                    var name = string.Format("thread {0}", i + 1);
                    Thread.CurrentThread.Name = name;

                    numberOfOperations.Times(() =>
                    {
                        var aggId = getRandomAggregateRootId();

                        _cirqus.ProcessCommand(new TakeNextStepCommand(aggId));
                    });

                    Console.WriteLine("Thread {0} done", name);
                }))
                .ToList();

            threads.ForEach(t => t.Start());
            threads.ForEach(t => t.Join());
        }


        public class TakeNextStepCommand : Command<ProgrammerAggregate>
        {
            public TakeNextStepCommand(string aggregateRootId)
                : base(aggregateRootId)
            {
            }

            public override void Execute(ProgrammerAggregate aggregateRoot)
            {
                aggregateRoot.TakeNextStep();
            }
        }

        public class ProgrammerAggregate : AggregateRoot, IEmit<FinishedEducation>, IEmit<LearnedAboutFunctionalProgramming>, IEmit<IncreasedKnowledge>
        {
            enum ProgrammerState
            {
                Born,
                Educated,
                Knowing
            }

            ProgrammerState currentState;
            int knowledge;

            public string GetCurrentState()
            {
                return currentState.ToString();
            }

            public void TakeNextStep()
            {
                switch (currentState)
                {
                    case ProgrammerState.Born:
                        Emit(new FinishedEducation());
                        break;
                    case ProgrammerState.Educated:
                        Emit(new LearnedAboutFunctionalProgramming());
                        break;
                    case ProgrammerState.Knowing:
                        Emit(new IncreasedKnowledge());
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(string.Format("Unknown current state: {0}", currentState));
                }
            }

            public void Apply(FinishedEducation e)
            {
                currentState = ProgrammerState.Educated;
            }

            public void Apply(LearnedAboutFunctionalProgramming e)
            {
                currentState = ProgrammerState.Knowing;
            }

            public void Apply(IncreasedKnowledge e)
            {
                knowledge++;
            }
        }

        public class FinishedEducation : DomainEvent<ProgrammerAggregate> { }

        public class LearnedAboutFunctionalProgramming : DomainEvent<ProgrammerAggregate> { }

        public class IncreasedKnowledge : DomainEvent<ProgrammerAggregate> { }
    }
}
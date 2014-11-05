using System;
using d60.Cirqus.Aggregates;
using d60.Cirqus.Commands;
using d60.Cirqus.Events;
using d60.Cirqus.Logging;
using d60.Cirqus.Logging.Console;
using d60.Cirqus.Serialization;
using d60.Cirqus.Testing.Internals;
using d60.Cirqus.Tests.Extensions;
using d60.Cirqus.Tests.Stubs;
using NUnit.Framework;

namespace d60.Cirqus.Tests.Integration
{
    [TestFixture, Description(@"Simulates the entire pipeline of event processing:

1. command comes in
2. command is mapped to an operation on an aggregate root
3. new unit of work is created
4. operation is invoked, events are collected
5. collected events are submitted atomically to event store
    a) on duplicate key error: go back to (3)
6. await synchronous views
7. publish events for consumption by chaser views
")]
    public class SimpleScenario : FixtureBase
    {
        CommandProcessor _cirqus;
        DefaultAggregateRootRepository _aggregateRootRepository;
        readonly JsonDomainEventSerializer _domainEventSerializer = new JsonDomainEventSerializer();

        protected override void DoSetUp()
        {
            var eventStore = new InMemoryEventStore(_domainEventSerializer);

            _aggregateRootRepository = new DefaultAggregateRootRepository(eventStore, _domainEventSerializer);

            var viewManager = new ConsoleOutEventDispatcher();

            _cirqus = new CommandProcessor(eventStore, _aggregateRootRepository, viewManager, _domainEventSerializer)
            {
                Options =
                {
                    MaxRetries = 3,

                    PurgeExistingViews = true,

                    GlobalLoggerFactory = new ConsoleLoggerFactory(minLevel: Logger.Level.Warn),

                    DomainExceptionTypes =
                    {
                        typeof(ApplicationException)
                    },
                }
            };

            RegisterForDisposal(_cirqus);
        }

        [Test]
        public void RunEntirePipelineAndProbePrivatesForMultipleAggregates()
        {
            // verify that fresh aggregates are delivered
            Assert.That(_aggregateRootRepository.Get<ProgrammerAggregate>("rootid1").AggregateRoot.GetCurrentState(), Is.EqualTo("Born"));
            Assert.That(_aggregateRootRepository.Get<ProgrammerAggregate>("rootid2").AggregateRoot.GetCurrentState(), Is.EqualTo("Born"));

            _cirqus.ProcessCommand(new TakeNextStepCommand("rootid1"));

            // verify that the command hit the first aggregate
            Assert.That(_aggregateRootRepository.Get<ProgrammerAggregate>("rootid1").AggregateRoot.GetCurrentState(), Is.EqualTo("Educated"));
            Assert.That(_aggregateRootRepository.Get<ProgrammerAggregate>("rootid2").AggregateRoot.GetCurrentState(), Is.EqualTo("Born"));

            _cirqus.ProcessCommand(new TakeNextStepCommand("rootid2"));

            // verify that the command hit the other aggregate
            Assert.That(_aggregateRootRepository.Get<ProgrammerAggregate>("rootid1").AggregateRoot.GetCurrentState(), Is.EqualTo("Educated"));
            Assert.That(_aggregateRootRepository.Get<ProgrammerAggregate>("rootid2").AggregateRoot.GetCurrentState(), Is.EqualTo("Educated"));

            _cirqus.ProcessCommand(new TakeNextStepCommand("rootid2"));

            // verify that the command hit the other aggregate
            Assert.That(_aggregateRootRepository.Get<ProgrammerAggregate>("rootid1").AggregateRoot.GetCurrentState(), Is.EqualTo("Educated"));
            Assert.That(_aggregateRootRepository.Get<ProgrammerAggregate>("rootid2").AggregateRoot.GetCurrentState(), Is.EqualTo("Knowing"));

            _cirqus.ProcessCommand(new TakeNextStepCommand("rootid1"));

            // verify that the command hit the first aggregate
            Assert.That(_aggregateRootRepository.Get<ProgrammerAggregate>("rootid1").AggregateRoot.GetCurrentState(), Is.EqualTo("Knowing"));
            Assert.That(_aggregateRootRepository.Get<ProgrammerAggregate>("rootid2").AggregateRoot.GetCurrentState(), Is.EqualTo("Knowing"));

            _cirqus.ProcessCommand(new TakeNextStepCommand("rootid1"));

            // verify that we have hit the end state
            Assert.That(_aggregateRootRepository.Get<ProgrammerAggregate>("rootid1").AggregateRoot.GetCurrentState(), Is.EqualTo("Knowing"));
            Assert.That(_aggregateRootRepository.Get<ProgrammerAggregate>("rootid2").AggregateRoot.GetCurrentState(), Is.EqualTo("Knowing"));
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

        public class ProgrammerAggregate : AggregateRoot, IEmit<FinishedEducation>, IEmit<LearnedAboutFunctionalProgramming>
        {
            enum ProgrammerState
            {
                Born,
                Educated,
                Knowing
            }

            ProgrammerState currentState;
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
                        // just keep on knowing
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
        }

        public class FinishedEducation : DomainEvent<ProgrammerAggregate>
        {

        }

        public class LearnedAboutFunctionalProgramming : DomainEvent<ProgrammerAggregate>
        {

        }
    }
}
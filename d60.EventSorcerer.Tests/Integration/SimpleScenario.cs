using System;
using d60.EventSorcerer.Aggregates;
using d60.EventSorcerer.Commands;
using d60.EventSorcerer.Config;
using d60.EventSorcerer.Events;
using d60.EventSorcerer.Tests.Stubs;
using NUnit.Framework;

namespace d60.EventSorcerer.Tests.Integration
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
        EventSorcererConfig _eventSorcerer;
        InMemoryAggregateRootRepository _aggregateRootRepository;

        protected override void DoSetUp()
        {
            var eventStore = new InMemoryEventStore();
            
            _aggregateRootRepository = new InMemoryAggregateRootRepository();
            
            var sequenceNumberGenerator = new TestSequenceNumberGenerator(1);
            var commandMapper = new CommandMapper()
                .Map<TakeNextStepCommand, ProgrammerAggregate>((c, a) => a.TakeNextStep());

            var viewManager = new ConsoleOutEventDispatcher();

            _eventSorcerer = new EventSorcererConfig(eventStore, _aggregateRootRepository, commandMapper, sequenceNumberGenerator, viewManager);
        }

        [Test]
        public void RunEntirePipelineAndProbePrivatesForMultipleAggregates()
        {
            var firstAggregateRootId = Guid.NewGuid();
            var nextAggregateRootId = Guid.NewGuid();


            // verify that fresh aggregates are delivered
            Assert.That(_aggregateRootRepository.Get<ProgrammerAggregate>(firstAggregateRootId).GetCurrentState(), Is.EqualTo("Born"));
            Assert.That(_aggregateRootRepository.Get<ProgrammerAggregate>(nextAggregateRootId).GetCurrentState(), Is.EqualTo("Born"));

            _eventSorcerer.ProcessCommand(new TakeNextStepCommand(firstAggregateRootId));

            // verify that the command hit the first aggregate
            Assert.That(_aggregateRootRepository.Get<ProgrammerAggregate>(firstAggregateRootId).GetCurrentState(), Is.EqualTo("Educated"));
            Assert.That(_aggregateRootRepository.Get<ProgrammerAggregate>(nextAggregateRootId).GetCurrentState(), Is.EqualTo("Born"));

            _eventSorcerer.ProcessCommand(new TakeNextStepCommand(nextAggregateRootId));

            // verify that the command hit the other aggregate
            Assert.That(_aggregateRootRepository.Get<ProgrammerAggregate>(firstAggregateRootId).GetCurrentState(), Is.EqualTo("Educated"));
            Assert.That(_aggregateRootRepository.Get<ProgrammerAggregate>(nextAggregateRootId).GetCurrentState(), Is.EqualTo("Educated"));

            _eventSorcerer.ProcessCommand(new TakeNextStepCommand(nextAggregateRootId));

            // verify that the command hit the other aggregate
            Assert.That(_aggregateRootRepository.Get<ProgrammerAggregate>(firstAggregateRootId).GetCurrentState(), Is.EqualTo("Educated"));
            Assert.That(_aggregateRootRepository.Get<ProgrammerAggregate>(nextAggregateRootId).GetCurrentState(), Is.EqualTo("Knowing"));

            _eventSorcerer.ProcessCommand(new TakeNextStepCommand(firstAggregateRootId));

            // verify that the command hit the first aggregate
            Assert.That(_aggregateRootRepository.Get<ProgrammerAggregate>(firstAggregateRootId).GetCurrentState(), Is.EqualTo("Knowing"));
            Assert.That(_aggregateRootRepository.Get<ProgrammerAggregate>(nextAggregateRootId).GetCurrentState(), Is.EqualTo("Knowing"));

            _eventSorcerer.ProcessCommand(new TakeNextStepCommand(firstAggregateRootId));

            // verify that we have hit the end state
            Assert.That(_aggregateRootRepository.Get<ProgrammerAggregate>(firstAggregateRootId).GetCurrentState(), Is.EqualTo("Knowing"));
            Assert.That(_aggregateRootRepository.Get<ProgrammerAggregate>(nextAggregateRootId).GetCurrentState(), Is.EqualTo("Knowing"));
        }

        public class TakeNextStepCommand : Command<ProgrammerAggregate>
        {
            public TakeNextStepCommand(Guid aggregateRootId) : base(aggregateRootId)
            {
            }
        }

        public class ProgrammerAggregate : AggregateRoot
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
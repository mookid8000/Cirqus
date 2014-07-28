using System;
using System.Linq;
using d60.EventSorcerer.Aggregates;
using d60.EventSorcerer.Commands;
using d60.EventSorcerer.Config;
using d60.EventSorcerer.Events;
using d60.EventSorcerer.Tests.Stubs;
using NUnit.Framework;

namespace d60.EventSorcerer.Tests.Commands
{
    [TestFixture]
    public class TestCustomMetadata : FixtureBase
    {
        EventSorcererConfig _eventSorcerer;
        InMemoryAggregateRootRepository _aggregateRootRepository;
        InMemoryEventStore _eventStore;

        protected override void DoSetUp()
        {
            _eventStore = new InMemoryEventStore();

            _aggregateRootRepository = new InMemoryAggregateRootRepository();

            var sequenceNumberGenerator = new TestSequenceNumberGenerator(1);
            var commandMapper = new CommandMapper()
                .Map<TakeNextStepCommand, ProgrammerAggregate>((c, a) => a.TakeNextStep());

            var viewManager = new ConsoleOutViewManager();

            _eventSorcerer = new EventSorcererConfig(_eventStore, _aggregateRootRepository, commandMapper, sequenceNumberGenerator, viewManager);
        }

        [Test]
        public void CopiesCustomHeadersToEmittedEvents()
        {
            var programmer1Id = Guid.NewGuid();
            var programmer2Id = Guid.NewGuid();
            var programmer3Id = Guid.NewGuid();

            const string tenantId = "tenant-id";

            _eventSorcerer.ProcessCommand(new TakeNextStepCommand(programmer1Id)
            {
                Meta = {{tenantId, "1"}}
            });
            
            _eventSorcerer.ProcessCommand(new TakeNextStepCommand(programmer2Id)
            {
                Meta = {{tenantId, "2"}}
            });
            
            _eventSorcerer.ProcessCommand(new TakeNextStepCommand(programmer3Id)
            {
                Meta = {{tenantId, "2"}}
            });

            var allEvents = _eventStore.ToList();

            Assert.That(allEvents.Count, Is.EqualTo(3));
            Assert.That(allEvents.Count(e => int.Parse(e.Meta[tenantId].ToString()) == 1), Is.EqualTo(1));
            Assert.That(allEvents.Count(e => int.Parse(e.Meta[tenantId].ToString()) == 2), Is.EqualTo(2));
        }

        public class TakeNextStepCommand : Command<ProgrammerAggregate>
        {
            public TakeNextStepCommand(Guid aggregateRootId)
                : base(aggregateRootId)
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
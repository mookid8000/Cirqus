using System;
using d60.EventSorcerer.Aggregates;
using d60.EventSorcerer.Events;
using NUnit.Framework;
using TestContext = d60.EventSorcerer.TestHelpers.TestContext;

namespace d60.EventSorcerer.Tests.Bugs
{
    [TestFixture]
    public class VerifyThatNewlyCreatedAggregateRootsAreAvailableForLoading : FixtureBase
    {
        TestContext _context;

        protected override void DoSetUp()
        {
            _context = new TestContext();
        }

        [Test]
        public void ItHasBeenFixed()
        {
            // arrange
            var counterpartId = Guid.NewGuid();
            var counterpart = _context.Get<Counterpart>(counterpartId);
            // create fresh counterpart AR by setting its name
            counterpart.SetName("muggie");

            var contractId = Guid.NewGuid();
            var contract = _context.Get<Contract>(contractId);

            // now make the contract do something that causes the counterpart to be loaded
            // act
            contract.AssignWith(counterpart);
            
            _context.Commit();

            // assert
            var reloadedContract = _context.Get<Contract>(contractId);
            Assert.That(reloadedContract.AssignedCounterpartName, Is.EqualTo("muggie"));
        }


        public class Counterpart : AggregateRoot, IEmit<CounterpartNamed>
        {
            public string Name { get; private set; }

            public void SetName(string name)
            {
                Emit(new CounterpartNamed { Name = name });
            }

            public void Apply(CounterpartNamed e)
            {
                Name = e.Name;
            }
        }

        public class CounterpartNamed : DomainEvent<Counterpart>
        {
            public string Name { get; set; }
        }

        public class Contract : AggregateRoot, IEmit<ContractAssignedToCounterpart>
        {
            public string AssignedCounterpartName { get; private set; }

            public void AssignWith(Counterpart counterpart)
            {
                Emit(new ContractAssignedToCounterpart { CounterpartId = counterpart.Id });
            }

            public void Apply(ContractAssignedToCounterpart e)
            {
                AssignedCounterpartName = Load<Counterpart>(e.CounterpartId).Name;
            }
        }

        public class ContractAssignedToCounterpart : DomainEvent<Contract>
        {
            public Guid CounterpartId { get; set; }
        }
    }
}
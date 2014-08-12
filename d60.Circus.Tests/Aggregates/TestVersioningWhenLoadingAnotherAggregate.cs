using System;
using d60.Circus.Aggregates;
using d60.Circus.Events;
using NUnit.Framework;
using TestContext = d60.Circus.TestHelpers.TestContext;

namespace d60.Circus.Tests.Aggregates
{
    [TestFixture]
    public class TestVersioningWhenLoadingAnotherAggregate : FixtureBase
    {
        TestContext _context;

        protected override void DoSetUp()
        {
            _context = new TestContext();
        }

        [Test]
        public void GetsTheRightVersionWhenLoadingDuringReplay()
        {
            var counterpartyId = Guid.NewGuid();
            var contractId = Guid.NewGuid();

            var counterparty = _context.Get<Counterparty>(counterpartyId);
            counterparty.SetName("joe");
            _context.Commit();

            var contract = _context.Get<Contract>(contractId);
            contract.AssignToCounterparty(counterpartyId);
            _context.Commit();

            counterparty = _context.Get<Counterparty>(counterpartyId);
            counterparty.SetName("moe");
            _context.Commit();

            var loadedContract = _context.Get<Contract>(contractId);
            Assert.That(loadedContract.NameOfCounterparty, Is.EqualTo("joe"));
        }

        public class Contract : AggregateRoot, IEmit<ContractAssignedToCounterparty>
        {
            public string NameOfCounterparty { get; set; }
            public void AssignToCounterparty(Guid counterpartyId)
            {
                Emit(new ContractAssignedToCounterparty { CounterpartyId = counterpartyId });
            }

            public void Apply(ContractAssignedToCounterparty e)
            {
                var counterparty = Load<Counterparty>(e.CounterpartyId);

                NameOfCounterparty = counterparty.Name;
            }
        }

        public class Counterparty : AggregateRoot, IEmit<CounterpartyNameChanged>
        {
            public string Name { get; set; }
            public void SetName(string name)
            {
                Emit(new CounterpartyNameChanged {NewName = name});
            }
            public void Apply(CounterpartyNameChanged e)
            {
                Name = e.NewName;
            }
        }

        public class ContractAssignedToCounterparty : DomainEvent<Contract>
        {
            public Guid CounterpartyId { get; set; }
        }

        public class CounterpartyNameChanged : DomainEvent<Counterparty>
        {
            public string NewName { get; set; }
        }
    }
}
using System;
using d60.Cirqus.Aggregates;
using d60.Cirqus.Events;
using NUnit.Framework;
using TestContext = d60.Cirqus.TestHelpers.TestContext;

namespace d60.Cirqus.Tests.Aggregates
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

            using (var uow = _context.BeginUnitOfWork())
            {
                var counterparty = uow.Get<Counterparty>(counterpartyId);
                counterparty.SetName("joe");
                uow.Commit();
            }

            using (var uow = _context.BeginUnitOfWork())
            {
                var contract = uow.Get<Contract>(contractId);
                contract.AssignToCounterparty(counterpartyId);
                uow.Commit();
            }

            using (var uow = _context.BeginUnitOfWork())
            {
                var counterparty = uow.Get<Counterparty>(counterpartyId);
                counterparty.SetName("moe");
                uow.Commit();
            }

            var loadedContract = _context.BeginUnitOfWork().Get<Contract>(contractId);
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
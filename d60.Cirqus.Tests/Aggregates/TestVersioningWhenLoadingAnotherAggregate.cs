using System;
using d60.Cirqus.Aggregates;
using d60.Cirqus.Events;
using NUnit.Framework;
using TestContext = d60.Cirqus.Testing.TestContext;

namespace d60.Cirqus.Tests.Aggregates
{
    [TestFixture]
    public class TestVersioningWhenLoadingAnotherAggregate : FixtureBase
    {
        TestContext _context;

        protected override void DoSetUp()
        {
            _context = RegisterForDisposal(TestContext.Create());
        }

        [Test]
        public void GetsTheRightVersionWhenLoadingDuringReplay()
        {
            using (var uow = _context.BeginUnitOfWork())
            {
                var counterparty = uow.Load<Counterparty>("counterparty1");
                counterparty.SetName("joe");
                uow.Commit();
            }

            using (var uow = _context.BeginUnitOfWork())
            {
                var contract = uow.Load<Contract>("contract1");
                contract.AssignToCounterparty("counterparty1");
                uow.Commit();
            }

            using (var uow = _context.BeginUnitOfWork())
            {
                var counterparty = uow.Load<Counterparty>("counterparty1");
                counterparty.SetName("moe");
                uow.Commit();
            }

            var loadedContract = _context.BeginUnitOfWork().Load<Contract>("contract1");
            Assert.That(loadedContract.NameOfCounterparty, Is.EqualTo("joe"));
        }

        public class Contract : AggregateRoot, IEmit<ContractAssignedToCounterparty>
        {
            public string NameOfCounterparty { get; set; }
            
            public void AssignToCounterparty(string counterpartyId)
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
            public string CounterpartyId { get; set; }
        }

        public class CounterpartyNameChanged : DomainEvent<Counterparty>
        {
            public string NewName { get; set; }
        }
    }
}
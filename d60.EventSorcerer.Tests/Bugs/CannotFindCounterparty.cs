using System;
using d60.EventSorcerer.Aggregates;
using d60.EventSorcerer.Events;
using d60.EventSorcerer.TestHelpers;
using d60.EventSorcerer.Tests.Stubs;
using d60.EventSorcerer.Views.Basic;
using d60.EventSorcerer.Views.Basic.Locators;
using NUnit.Framework;

namespace d60.EventSorcerer.Tests.Bugs
{
    [TestFixture]
    public class CannotFindCounterparty : FixtureBase
    {
        InMemoryEventStore _eventStore;
        BasicEventDispatcher _eventDispatcher;
        TestSequenceNumberGenerator _sequenceNumberGenerator;
        BasicAggregateRootRepository _aggregateRootRepository;

        protected override void DoSetUp()
        {
            _eventStore = new InMemoryEventStore();

            _aggregateRootRepository = new BasicAggregateRootRepository(_eventStore);

            _eventDispatcher = new BasicEventDispatcher(_aggregateRootRepository, new InMemoryViewManager<SomeView>());

            _sequenceNumberGenerator = new TestSequenceNumberGenerator();
        }

        [Test]
        public void DoesNotCrash()
        {
            var counterpartyId = Guid.NewGuid();
            var contractId = Guid.NewGuid();

            SaveEvent(counterpartyId, new CounterpartyCreated { Name = "joe" });
            SaveEvent(contractId, new ContractSigned { CounterpartyId = counterpartyId });

            var contract = _aggregateRootRepository.Get<Contract>(contractId);
        }

        void SaveEvent(Guid aggregateRootId, DomainEvent e)
        {
            e.Meta[DomainEvent.MetadataKeys.AggregateRootId] = aggregateRootId;
            e.Meta[DomainEvent.MetadataKeys.SequenceNumber] = _sequenceNumberGenerator.Next(aggregateRootId);
            _eventStore.Save(Guid.NewGuid(), new[] { e });
        }

        public class CounterpartyCreated : DomainEvent<Counterparty>
        {
            public string Name { get; set; }
        }

        public class Counterparty : AggregateRoot
        {

        }

        public class Contract : AggregateRoot, IEmit<ContractSigned>
        {
            public Guid OwnerCounterpartyId { get; set; }
            public void Apply(ContractSigned e)
            {
                OwnerCounterpartyId = e.CounterpartyId;
            }
        }

        public class ContractSigned : DomainEvent<Contract>
        {
            public Guid CounterpartyId { get; set; }
        }

        public class SomeView : IView<InstancePerAggregateRootLocator>, ISubscribeTo<ContractSigned>
        {
            public string Id { get; set; }
            public void Handle(IViewContext context, ContractSigned domainEvent)
            {
            }
        }
    }
}
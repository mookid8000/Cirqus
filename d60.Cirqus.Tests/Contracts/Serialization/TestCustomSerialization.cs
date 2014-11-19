using System;
using System.Collections.Generic;
using d60.Cirqus.Aggregates;
using d60.Cirqus.Commands;
using d60.Cirqus.Config;
using d60.Cirqus.Events;
using d60.Cirqus.Extensions;
using d60.Cirqus.Serialization;
using d60.Cirqus.Tests.Contracts.EventStore;
using d60.Cirqus.Tests.Contracts.EventStore.Factories;
using d60.Cirqus.Views.ViewManagers;
using d60.Cirqus.Views.ViewManagers.Locators;
using NUnit.Framework;

namespace d60.Cirqus.Tests.Contracts.Serialization
{
    [Description("Contract test for event stores. Verifies that event store implementation and sequence number generation works in tandem")]
    [TestFixture(typeof(MongoDbEventStoreFactory), Category = TestCategories.MongoDb)]
    [TestFixture(typeof(InMemoryEventStoreFactory))]
    [TestFixture(typeof(MsSqlEventStoreFactory), Category = TestCategories.MsSql)]
    [TestFixture(typeof(PostgreSqlEventStoreFactory), Category = TestCategories.PostgreSql)]
    [TestFixture(typeof(NtfsEventStoreFactory))]
    [TestFixture(typeof(SQLiteEventStoreFactory))]
    [TestFixture(typeof(RavenDBEventStoreFactory))]
    public class CustomSerilization<TEventStoreFactory> : FixtureBase where TEventStoreFactory : IEventStoreFactory, new()
    {
        ICommandProcessor _commandProcessor;
        InMemoryViewManager<LeView> _viewManager;

        protected override void DoSetUp()
        {
            var factory = new TEventStoreFactory();

            _viewManager = new InMemoryViewManager<LeView>();

            _commandProcessor = CommandProcessor.With()
                .EventStore(e => e.Registrar.Register(c => factory.GetEventStore()))
                .EventDispatcher(e => e.UseViewManagerEventDispatcher(_viewManager))
                .Options(o => o.UseCustomDomainEventSerializer(new BinaryDomainEventSerializer()))
                .Create();

            RegisterForDisposal(_commandProcessor);
        }

        [Test]
        public void WorksWithCustomSerializer()
        {
            _commandProcessor.ProcessCommand(new LeCommand("rootid"));
            _commandProcessor.ProcessCommand(new LeCommand("rootid"));
            var lastResult = _commandProcessor.ProcessCommand(new LeCommand("rootid"));

            _viewManager.WaitUntilProcessed(lastResult, TimeSpan.FromSeconds(10)).Wait();
            var view = _viewManager.Load(GlobalInstanceLocator.GetViewInstanceId());

            Assert.That(view.SecretBizTimes.Length, Is.EqualTo(3));
        }

        public class LeView : IViewInstance<GlobalInstanceLocator>, ISubscribeTo<DomainEvent<Root>>
        {
            public string Id { get; set; }
            public long LastGlobalSequenceNumber { get; set; }
            public DateTime[] SecretBizTimes { get; set; }
            public void Handle(IViewContext context, DomainEvent<Root> domainEvent)
            {
                SecretBizTimes = context
                    .Load<Root>(domainEvent.GetAggregateRootId())
                    .SecretBizTimes
                    .ToArray();
            }
        }

        public class Root : AggregateRoot, IEmit<LeEvent>
        {
            readonly List<DateTime> _secretBizTimes = new List<DateTime>();

            public void DoStuff()
            {
                Emit(new LeEvent { SecretBizTime = DateTime.Now });
            }

            public void Apply(LeEvent e)
            {
                _secretBizTimes.Add(e.SecretBizTime);
            }

            public List<DateTime> SecretBizTimes
            {
                get { return _secretBizTimes; }
            }
        }

        public class LeCommand : Command<Root>
        {
            public LeCommand(string aggregateRootId)
                : base(aggregateRootId)
            {
            }

            public override void Execute(Root aggregateRoot)
            {
                aggregateRoot.DoStuff();
            }
        }

        [Serializable]
        public class LeEvent : DomainEvent<Root>
        {
            public DateTime SecretBizTime { get; set; }
        }
    }

}
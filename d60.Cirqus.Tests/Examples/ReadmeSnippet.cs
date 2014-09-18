using System;
using System.Collections.Generic;
using d60.Cirqus.Aggregates;
using d60.Cirqus.Commands;
using d60.Cirqus.Events;
using d60.Cirqus.Extensions;
using d60.Cirqus.MsSql.Events;
using d60.Cirqus.Tests.MsSql;
using d60.Cirqus.Views.ViewManagers;
using d60.Cirqus.Views.ViewManagers.Locators;
using NUnit.Framework;
using ViewManagerEventDispatcher = d60.Cirqus.Views.ViewManagers.Old.ViewManagerEventDispatcher;

namespace d60.Cirqus.Tests.Examples
{
    [TestFixture]
    public class ReadmeSnippet : FixtureBase
    {
        protected override void DoSetUp()
        {
            MsSqlTestHelper.EnsureTestDatabaseExists();
        }

        [Test]
        public void TheSnippet()
        {
            MsSqlTestHelper.DropTable("events");
            MsSqlTestHelper.DropTable("counters");

            var eventStore = new MsSqlEventStore(MsSqlTestHelper.ConnectionString, "events", automaticallyCreateSchema: true);
            var aggregateRootRepository = new DefaultAggregateRootRepository(eventStore);
            var viewManager = new Cirqus.MsSql.Views.Old.MsSqlViewManager<CounterView>(MsSqlTestHelper.ConnectionString, "counters", automaticallyCreateSchema: true);

            var eventDispatcher = new ViewManagerEventDispatcher(aggregateRootRepository, viewManager);

            var processor = new CommandProcessor(eventStore, aggregateRootRepository, eventDispatcher);

            RegisterForDisposal(processor);

            processor.Initialize();

            var aggregateRootId = Guid.NewGuid();
            processor.ProcessCommand(new IncrementCounter(aggregateRootId, 1));
            processor.ProcessCommand(new IncrementCounter(aggregateRootId, 2));
            processor.ProcessCommand(new IncrementCounter(aggregateRootId, 3));
            processor.ProcessCommand(new IncrementCounter(aggregateRootId, 5));
            processor.ProcessCommand(new IncrementCounter(aggregateRootId, 8));
        }

        public class IncrementCounter : Command<Counter>
        {
            public IncrementCounter(Guid aggregateRootId, int delta)
                : base(aggregateRootId)
            {
                Delta = delta;
            }

            public int Delta { get; private set; }

            public override void Execute(Counter aggregateRoot)
            {
                aggregateRoot.Increment(Delta);
            }
        }

        public class CounterIncremented : DomainEvent<Counter>
        {
            public CounterIncremented(int delta)
            {
                Delta = delta;
            }

            public int Delta { get; private set; }
        }

        public class Counter : AggregateRoot, IEmit<CounterIncremented>
        {
            int _currentValue;

            public void Increment(int delta)
            {
                Emit(new CounterIncremented(delta));
            }

            public void Apply(CounterIncremented e)
            {
                _currentValue += e.Delta;
            }

            public int CurrentValue
            {
                get { return _currentValue; }
            }

            public double GetSecretBizValue()
            {
                return CurrentValue%2 == 0
                    ? Math.PI
                    : CurrentValue;
            }
        }

        public class CounterView : IViewInstance<InstancePerAggregateRootLocator>, ISubscribeTo<CounterIncremented>
        {
            public CounterView()
            {
                SomeRecentBizValues = new List<double>();
            }

            public string Id { get; set; }

            public long LastGlobalSequenceNumber { get; set; }

            public int CurrentValue { get; set; }

            public double SecretBizValue { get; set; }

            public List<double> SomeRecentBizValues { get; set; }

            public void Handle(IViewContext context, CounterIncremented domainEvent)
            {
                CurrentValue += domainEvent.Delta;

                var counter = context.Load<Counter>(domainEvent.GetAggregateRootId(), domainEvent.GetGlobalSequenceNumber());

                SecretBizValue = counter.GetSecretBizValue();

                SomeRecentBizValues.Add(SecretBizValue);

                // trim to 10 most recent biz values
                while(SomeRecentBizValues.Count > 10) 
                    SomeRecentBizValues.RemoveAt(0);
            }
        }
    }

}
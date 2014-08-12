using System;
using System.Collections.Generic;
using d60.Circus.Aggregates;
using d60.Circus.Commands;
using d60.Circus.Config;
using d60.Circus.Events;
using d60.Circus.Extensions;
using d60.Circus.MsSql.Events;
using d60.Circus.MsSql.Views;
using d60.Circus.Tests.MsSql;
using d60.Circus.Views.Basic;
using d60.Circus.Views.Basic.Locators;
using NUnit.Framework;

namespace d60.Circus.Tests.Examples
{
    [TestFixture]
    public class ReadmeSnippet : FixtureBase
    {
        protected override void DoSetUp()
        {
            TestSqlHelper.EnsureTestDatabaseExists();
        }

        [Test]
        public void TheSnippet()
        {
            TestSqlHelper.DropTable("events");
            TestSqlHelper.DropTable("counters");

            var eventStore = new MsSqlEventStore(TestSqlHelper.ConnectionString, "events", automaticallyCreateSchema: true);
            var aggregateRootRepository = new DefaultAggregateRootRepository(eventStore);
            var viewManager = new MsSqlViewManager<CounterView>(TestSqlHelper.ConnectionString, "counters", automaticallyCreateSchema: true);

            var eventDispatcher = new BasicEventDispatcher(aggregateRootRepository, viewManager);

            var processor = new CommandProcessor(eventStore, aggregateRootRepository, eventDispatcher);
            
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
# d60 Circus

Simple but powerful event sourcing + CQRS kit. 

Provides a model for creating commands, aggregate roots, events, and views,
tying it all up in one simple and neat `CommandProcessor`.

### How simple?

You do this:

    var processor = new CommandProcessor(...);

(of course satisfying the processor's few dependencies), and then you can do this:

    processor.ProcessCommand(myCommand);

and then everything will flow from there.

### Full example

This is how you can set up a fully functioning command processor, including a view:

    // this is the origin of truth - let's keep it in SQL Server!
    var eventStore = new MsSqlEventStore("sqltestdb", "events", automaticallyCreateSchema: true);

    // aggregate roots are simply built when needed by replaying events for the requested root
    var aggregateRootRepository = new DefaultAggregateRootRepository(eventStore);

    // configure one single view manager in another table in our SQL Server
    var viewManager = new MsSqlViewManager<CounterView>("sqltestdb", "counters", automaticallyCreateSchema: true);

    // Circus will deliver emitted events to the event dispatcher when they have been persisted
    var eventDispatcher = new BasicEventDispatcher(aggregateRootRepository, viewManager);

    // we can create the processor now
    var processor = new CommandProcessor(eventStore, aggregateRootRepository, eventDispatcher);

This is an example of a command whose purpose it is to instruct the `Counter` aggregate root to increment itself by
some specific value, as indicated by the given `delta` parameter:

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

Note how the command indicates the type and ID of the aggregate root to address, as well as an `Execute` method that
will be invoked by the framework. Let's take a look at `Counter` - aggregate roots must be based on the
`AggregateRoot` base class and must of course follow the _emit/apply_ pattern for mutating themselves - it looks like this:

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

As you can see, the command's `Execute` method will invoke the `Increment(delta)` method on the root, which
in turn will emit a `CounterIncremented` event, which simply looks like this:

    public class CounterIncremented : DomainEvent<Counter>
    {
        public CounterIncremented(int delta)
        {
            Delta = delta;
        }

        public int Delta { get; private set; }
    }

The event is immediately applied (via the root's `Apply` method that comes from implementing
`IEmit<CounterIncremented>`), and this is the place where the root is free
to mutate itself - in this case, we increment the private `_currentValue` variable, which serves to demonstrate
that aggregate roots are free to keep their privates private.

Note also how the aggregate root is capable of calculating a secret business value, which happens to alternate
between the counter's value and π, depending on whether the counter's value is odd or even.

Lastly, we have set up a `MsSqlViewManager` that operates on a `CounterView` that looks like this:

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

Views can define how processed events are mapped to view IDs via the `ViewLocator` implementation that closes the `IViewInstance<>`
interface - in this case, we're using `InstancePerAggregateRootLocator` which means that the aggregate root ID of the processed
event is simply used as the ID of the view, in turn resulting in one instance of the view per aggregate root.

In order to actually get to receive events, the view class must implement one or more `ISubscribeTo<>` interfaces - in this case,
we subscribe to `CounterIncremented` which requires that we implement the `Handle` method.

In addition to the two required properties, `Id` and `LastGLobalSequenceNumber`, we've added a property for the current value
of the counter, a property for the secret business value, and a list that can contain the 10 most recent business value.

Note how the `IViewContext` gives access to a `Load` method that can be used by the view to load aggregate roots if it needs to invoke
domain logic to extract certain values out of the domain (like e.g. our secret business value). Note also how
an aggregate root must be loaded by specifying a global sequence number which will serve as a "roof" for applied event, thus
ensuring that the loaded aggregate root has the version that corresponds to the time when the event was emitted, thus allowing for
consistent replaying of events. It also allows for poking back and forth in time ;)  (but that's another story....)
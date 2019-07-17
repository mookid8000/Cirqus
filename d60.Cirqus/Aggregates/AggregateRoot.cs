using System;
using System.Reflection;
using d60.Cirqus.Events;
using d60.Cirqus.Exceptions;
using d60.Cirqus.Extensions;
using d60.Cirqus.Numbers;

namespace d60.Cirqus.Aggregates
{
    /// <summary>
    /// This is the base class of aggregate roots. Derive from this in order to create an event-driven domain model root object that
    /// is capable of emitting its own events, building its state as a projection off of previously emitted events.
    /// </summary>
    public abstract class AggregateRoot
    {
        internal const int InitialAggregateRootSequenceNumber = -1;

        /// <summary>
        /// Gets the ID of the aggregate root. This one should be automatically set on the aggregate root instance provided by Cirqus
        /// </summary>
        public string Id { get; internal set; }

        internal IUnitOfWork UnitOfWork { get; set; }

        internal Metadata CurrentCommandMetadata { get; set; }

        internal void Initialize(string id)
        {
            Id = id;
        }

        internal void InvokeCreated()
        {
            Created();
        }

        internal protected virtual void EventEmitted(DomainEvent e) { }

        internal long CurrentSequenceNumber = InitialAggregateRootSequenceNumber;

        internal long GlobalSequenceNumberCutoff = long.MaxValue;

        internal ReplayState ReplayState = ReplayState.None;

        /// <summary>
        /// Method that is called when the aggregate root is first created, allowing you to emit that famous created event if you absolutely need it ;)
        /// </summary>
        protected virtual void Created() { }

        /// <summary>
        /// Gets whether this aggregate root has emitted any events. I.e. it will already be false after having used the <see cref="Created"/> method to emit one
        /// single "MyRootCreated" event.
        /// </summary>
        protected bool IsNew
        {
            get { return CurrentSequenceNumber == InitialAggregateRootSequenceNumber; }
        }

        /// <summary>
        /// Emits the given domain event, adding the aggregate root's <see cref="Id"/> and a sequence number to its metadata, along with some type information
        /// </summary>
        protected void Emit<TAggregateRoot>(DomainEvent<TAggregateRoot> e) where TAggregateRoot : AggregateRoot
        {
            if (e == null) throw new ArgumentNullException("e", "Can't emit null!");

            if (string.IsNullOrWhiteSpace(Id))
            {
                throw new InvalidOperationException(
                    string.Format(
                        "Attempted to emit event {0} from aggregate root {1}, but it has not yet been assigned an ID!",
                        e, GetType()));
            }

            var eventType = e.GetType();

            var emitterInterface = typeof(IEmit<>).MakeGenericType(eventType);
            if (!emitterInterface.IsAssignableFrom(GetType()))
            {
                throw new InvalidOperationException(
                    string.Format(
                        "Attempted to emit event {0} but the aggregate root {1} does not implement IEmit<{2}>",
                        e, GetType().Name, e.GetType().Name));
            }

            if (UnitOfWork == null)
            {
                throw new InvalidOperationException(string.Format("Attempted to emit event {0}, but the aggreate root does not have a unit of work!", e));
            }

            if (ReplayState != ReplayState.None)
            {
                throw new InvalidOperationException(string.Format("Attempted to emit event {0}, but the aggreate root's replay state is {1}! - events can only be emitted when the root is not applying events", e, ReplayState));
            }

            if (!typeof(TAggregateRoot).IsAssignableFrom(GetType()))
            {
                throw new InvalidOperationException(
                    string.Format("Attempted to emit event {0} which is owned by {1} from aggregate root of type {2}",
                        e, typeof(TAggregateRoot), GetType()));
            }

            var now = Time.UtcNow();
            var sequenceNumber = CurrentSequenceNumber + 1;

            e.Meta.Merge(CurrentCommandMetadata ?? new Metadata());
            e.Meta[DomainEvent.MetadataKeys.AggregateRootId] = Id;
            e.Meta[DomainEvent.MetadataKeys.TimeUtc] = now.ToString("u");
            e.Meta[DomainEvent.MetadataKeys.SequenceNumber] = sequenceNumber.ToString(Metadata.NumberCulture);

            e.Meta.TakeFromAttributes(eventType);
            e.Meta.TakeFromAttributes(GetType());

            try
            {
                ApplyEvent(e, ReplayState.EmitApply);
            }
            catch (Exception exception)
            {
                throw new ApplicationException(string.Format(@"Could not apply event {0} to {1} - please check the inner exception, and/or make sure that the aggregate root type is PUBLIC", e, this), exception);
            }

            UnitOfWork.AddEmittedEvent(this, e);
            EventEmitted(e);
        }

        internal void ApplyEvent(DomainEvent e, ReplayState replayState)
        {
            // tried caching here with a (aggRootType, eventType) lookup in two levels of concurrent dictionaries.... didn't provide significant perf boost

            var aggregateRootType = GetType();
            var domainEventType = e.GetType();

            var applyMethod = aggregateRootType.GetMethod("Apply", new[] { domainEventType });

            if (applyMethod == null)
            {
                throw new ApplicationException(
                    string.Format("Could not find appropriate Apply method on {0} - expects a method with a public void Apply({1}) signature",
                        aggregateRootType, domainEventType.FullName));
            }

            if (CurrentSequenceNumber + 1 != e.GetSequenceNumber())
            {
                throw new ApplicationException(
                    string.Format("Tried to apply event with sequence number {0} to aggregate root of type {1} with ID {2} with current sequence number {3}. Expected an event with sequence number {4}.",
                    e.GetSequenceNumber(), aggregateRootType, Id, CurrentSequenceNumber, CurrentSequenceNumber + 1));
            }

            var previousReplayState = ReplayState;

            try
            {
                ReplayState = replayState;

                if (ReplayState == ReplayState.ReplayApply)
                {
                    GlobalSequenceNumberCutoff = e.GetGlobalSequenceNumber();
                }

                applyMethod.Invoke(this, new object[] { e });

                GlobalSequenceNumberCutoff = long.MaxValue;
                ReplayState = previousReplayState;
            }
            catch (TargetInvocationException tae)
            {
                throw new ApplicationException(string.Format("Error when applying event {0} to aggregate root with ID {1}", e, Id), tae);
            }

            CurrentSequenceNumber = e.GetSequenceNumber();
        }

        public override string ToString()
        {
            return string.Format("{0} ({1})", GetType().Name, Id);
        }

        /// <summary>
        /// Creates another aggregate root with the specified <paramref name="aggregateRootId"/>. Will throw an exception if a root already exists with the specified ID.
        /// </summary>
        protected TAggregateRoot Create<TAggregateRoot>(string aggregateRootId) where TAggregateRoot : AggregateRoot
        {
            if (UnitOfWork == null)
            {
                throw new InvalidOperationException(
                    string.Format(
                        "Attempted to Load {0} with ID {1} from {2}, but it has not been initialized with a unit of work! The unit of work must be attached to the aggregate root in order to cache hydrated aggregate roots within the current unit of work.",
                        typeof(TAggregateRoot), aggregateRootId, GetType()));
            }

            if (ReplayState != ReplayState.None)
            {
                throw new InvalidOperationException(string.Format("Attempted to create new aggregate root of type {0} with ID {1}, but cannot create anything when replay state is {2}",
                    typeof(TAggregateRoot), aggregateRootId, ReplayState));
            }

            if (UnitOfWork.Exists(aggregateRootId, long.MaxValue))
            {
                throw new InvalidOperationException(string.Format("Cannot create aggregate root {0} with ID {1} because an instance with that ID already exists!",
                    typeof(TAggregateRoot), aggregateRootId));
            }

            var aggregateRoot = (TAggregateRoot)UnitOfWork.Get<TAggregateRoot>(aggregateRootId, long.MaxValue, createIfNotExists: true);

            aggregateRoot.CurrentCommandMetadata = CurrentCommandMetadata;

            aggregateRoot.InvokeCreated();

            return aggregateRoot;
        }

        /// <summary>
        /// Tries to load another aggregate root with the specified <paramref name="aggregateRootId"/>. Returns null if no root exists with that ID.
        /// Throws an <see cref="InvalidCastException"/> if a root was found, but its type was not compatible with the specified <typeparamref name="TAggregateRoot"/> type.
        /// </summary>
        protected TAggregateRoot TryLoad<TAggregateRoot>(string aggregateRootId) where TAggregateRoot : AggregateRoot
        {
            if (UnitOfWork == null)
            {
                throw new InvalidOperationException(
                    string.Format(
                        "Attempted to Load {0} with ID {1} from {2}, but it has not been initialized with a unit of work! The unit of work must be attached to the aggregate root in order to cache hydrated aggregate roots within the current unit of work.",
                        typeof(TAggregateRoot), aggregateRootId, GetType()));
            }

            var globalSequenceNumberCutoffToLookFor = ReplayState == ReplayState.ReplayApply
                ? GlobalSequenceNumberCutoff
                : long.MaxValue;

            AggregateRoot aggregateRoot;
            try
            {
                aggregateRoot = UnitOfWork
                    .Get<TAggregateRoot>(aggregateRootId, globalSequenceNumberCutoffToLookFor, createIfNotExists: false);
            }
            catch(AggregateRootNotFoundException)
            {
                return null;
            }

            try
            {
                aggregateRoot.CurrentCommandMetadata = CurrentCommandMetadata;
             
                return (TAggregateRoot) aggregateRoot;
            }
            catch (Exception exception)
            {
                throw new InvalidCastException(string.Format("Found aggregate root with ID {0} and type {1}, but the type is not compatible with the desired {2} type!",
                    aggregateRoot, aggregateRoot.GetType(), typeof(TAggregateRoot)), exception);
            }
        }

        /// <summary>
        /// Loads another aggregate root with the specified <paramref name="aggregateRootId"/>. Throws an exception if an aggregate root with that ID could not be found.
        /// Also throws an exception if an aggregate root instance was found, but the type was not compatible with the desired <typeparamref name="TAggregateRoot"/>
        /// </summary>
        protected TAggregateRoot Load<TAggregateRoot>(string aggregateRootId) where TAggregateRoot : AggregateRoot
        {
            if (UnitOfWork == null)
            {
                throw new InvalidOperationException(
                    string.Format(
                        "Attempted to Load {0} with ID {1} from {2}, but it has not been initialized with a unit of work! The unit of work must be attached to the aggregate root in order to cache hydrated aggregate roots within the current unit of work.",
                        typeof(TAggregateRoot), aggregateRootId, GetType()));
            }

            var globalSequenceNumberCutoffToLookFor = ReplayState == ReplayState.ReplayApply
                ? GlobalSequenceNumberCutoff
                : long.MaxValue;

            var aggregateRoot = UnitOfWork.Get<TAggregateRoot>(aggregateRootId, globalSequenceNumberCutoffToLookFor, createIfNotExists: false);

            if (!(aggregateRoot is TAggregateRoot))
            {
                throw new InvalidOperationException(string.Format("Attempted to load aggregate root with ID {0} as a {1}, but the concrete type is {2} which is not compatible",
                    aggregateRootId, typeof(TAggregateRoot), aggregateRootId.GetType()));
            }

            aggregateRoot.CurrentCommandMetadata = CurrentCommandMetadata;

            return (TAggregateRoot)aggregateRoot;
        }
    }
}
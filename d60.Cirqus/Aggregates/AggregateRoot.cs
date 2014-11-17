using System;
using System.Reflection;
using d60.Cirqus.Events;
using d60.Cirqus.Extensions;
using d60.Cirqus.Numbers;

namespace d60.Cirqus.Aggregates
{
    public abstract class AggregateRoot
    {
        internal const int InitialAggregateRootSequenceNumber = -1;

        internal IUnitOfWork UnitOfWork { get; set; }

        internal void Initialize(string id)
        {
            Id = id;
        }

        internal void InvokeCreated()
        {
            Created();
        }

        internal protected virtual void EventEmitted(DomainEvent e) { }

        public string Id { get; internal set; }

        internal long CurrentSequenceNumber = InitialAggregateRootSequenceNumber;

        internal long GlobalSequenceNumberCutoff = long.MaxValue;

        internal ReplayState ReplayState = ReplayState.None;

        protected virtual void Created() { }

        protected bool IsNew
        {
            get { return CurrentSequenceNumber == InitialAggregateRootSequenceNumber; }
        }

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

            if (typeof(TAggregateRoot) != GetType())
            {
                throw new InvalidOperationException(
                    string.Format("Attempted to emit event {0} which is owned by {1} from aggregate root of type {2}",
                        e, typeof(TAggregateRoot), GetType()));
            }

            var now = Time.UtcNow();
            var sequenceNumber = CurrentSequenceNumber + 1;

            e.Meta[DomainEvent.MetadataKeys.AggregateRootId] = Id;
            e.Meta[DomainEvent.MetadataKeys.TimeUtc] = now.ToString("u");
            e.Meta[DomainEvent.MetadataKeys.SequenceNumber] = sequenceNumber.ToString(Metadata.NumberCulture);

            e.Meta.TakeFromAttributes(eventType);
            e.Meta.TakeFromAttributes(GetType());

            try
            {
                ReplayState = ReplayState.EmitApply;

                ApplyEvent(e);

                ReplayState = ReplayState.None;
            }
            catch (Exception exception)
            {
                throw new ApplicationException(string.Format(@"Could not apply event {0} to {1} - please check the inner exception, and/or make sure that the aggregate root type is PUBLIC", e, this), exception);
            }

            UnitOfWork.AddEmittedEvent(e);
            EventEmitted(e);
        }

        internal void ApplyEvent(DomainEvent e)
        {
            // tried caching here with a (aggRootType, eventType) lookup in two levels of concurrent dictionaries.... didn't provide significant perf boost
            var applyMethod = GetType().GetMethod("Apply", new[] { e.GetType() });

            if (applyMethod == null)
            {
                throw new ApplicationException(
                    string.Format("Could not find appropriate Apply method - expects a method with a public void Apply({0}) signature",
                        e.GetType().FullName));
            }

            try
            {
                applyMethod.Invoke(this, new object[] { e });
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

        protected TAggregateRoot Create<TAggregateRoot>(string aggregateRootId) where TAggregateRoot : AggregateRoot, new()
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

            if (UnitOfWork.Exists<TAggregateRoot>(aggregateRootId, long.MaxValue))
            {
                throw new InvalidOperationException(string.Format("Cannot create aggregate root {0} with ID {1} because an instance with that ID already exists!",
                    typeof(TAggregateRoot), aggregateRootId));
            }

            return UnitOfWork.Get<TAggregateRoot>(aggregateRootId, long.MaxValue, createIfNotExists: true).AggregateRoot;
        }

        protected TAggregateRoot TryLoad<TAggregateRoot>(string aggregateRootId) where TAggregateRoot : AggregateRoot, new()
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

            try
            {
                var aggregateRootInfo = UnitOfWork
                    .Get<TAggregateRoot>(aggregateRootId, globalSequenceNumberCutoffToLookFor, createIfNotExists: false);

                return aggregateRootInfo.AggregateRoot;
            }
            catch
            {
                return null;
            }
        }

        protected TAggregateRoot Load<TAggregateRoot>(string aggregateRootId) where TAggregateRoot : AggregateRoot, new()
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

            return UnitOfWork.Get<TAggregateRoot>(aggregateRootId, globalSequenceNumberCutoffToLookFor, createIfNotExists: false).AggregateRoot;
        }
    }
}
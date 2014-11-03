using System;
using d60.Cirqus.Events;
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
            var sequenceNumber = ++CurrentSequenceNumber;

            e.Meta[DomainEvent.MetadataKeys.AggregateRootId] = Id;
            e.Meta[DomainEvent.MetadataKeys.TimeUtc] = now.ToString("u");
            e.Meta[DomainEvent.MetadataKeys.SequenceNumber] = sequenceNumber.ToString(Metadata.NumberCulture);
            e.Meta[DomainEvent.MetadataKeys.Owner] = GetOwnerFromType(GetType());

            e.Meta.TakeFromAttributes(eventType);
            e.Meta.TakeFromAttributes(GetType());

            try
            {
                ReplayState = ReplayState.EmitApply;

                var dynamicThis = (dynamic)this;

                dynamicThis.Apply((dynamic)e);

                ReplayState = ReplayState.None;
            }
            catch (Exception exception)
            {
                throw new ApplicationException(string.Format(@"Could not apply event {0} to {1} - please check the inner exception, and/or make sure that the aggregate root type is PUBLIC", e, this), exception);
            }

            UnitOfWork.AddEmittedEvent(e);
            EventEmitted(e);
        }

        internal static string GetOwnerFromType(Type aggregateRootType)
        {
            return string.Format("{0}, {1}", aggregateRootType.FullName, aggregateRootType.Assembly.GetName().Name);
        }

        public override string ToString()
        {
            return string.Format("{0} ({1})", GetType().Name, Id);
        }

        protected TAggregateRoot Load<TAggregateRoot>(string aggregateRootId, bool createIfNotExists = false) where TAggregateRoot : AggregateRoot, new()
        {
            if (UnitOfWork == null)
            {
                throw new InvalidOperationException(
                    string.Format(
                        "Attempted to Load {0} with ID {1} from {2}, but it has not been initialized with a unit of work! The unit of work must be attached to the aggregate root in order to cache hydrated aggregate roots within the current unit of work.",
                        typeof(TAggregateRoot), aggregateRootId, GetType()));
            }

            if (createIfNotExists && ReplayState != ReplayState.None)
            {
                throw new InvalidOperationException(string.Format("Attempted to load new aggregate root of type {0} with ID {1}, but cannot specify createIfNotExists = true when replay state is {2}",
                    typeof(TAggregateRoot), aggregateRootId, ReplayState));
            }

            var globalSequenceNumberCutoffToLookFor = ReplayState == ReplayState.ReplayApply
                ? GlobalSequenceNumberCutoff
                : long.MaxValue;

            var cachedAggregateRoot = UnitOfWork.Get<TAggregateRoot>(aggregateRootId, globalSequenceNumberCutoffToLookFor, createIfNotExists);

            if (cachedAggregateRoot != null)
            {
                return cachedAggregateRoot.AggregateRoot;
            }

            if (!createIfNotExists && !UnitOfWork.Exists<TAggregateRoot>(aggregateRootId, globalSequenceNumberCutoff: globalSequenceNumberCutoffToLookFor))
            {
                throw new ArgumentException(string.Format("Aggregate root {0} with ID {1} does not exist!", typeof(TAggregateRoot), aggregateRootId), "aggregateRootId");
            }

            var aggregateRootInfo = UnitOfWork.Get<TAggregateRoot>(aggregateRootId, globalSequenceNumberCutoff: globalSequenceNumberCutoffToLookFor);
            var aggregateRoot = aggregateRootInfo.AggregateRoot;

            var globalSequenceNumberToSaveUnder = ReplayState == ReplayState.None
                ? long.MaxValue
                : aggregateRootInfo.LastGlobalSeqNo;

            UnitOfWork.AddToCache(aggregateRoot, globalSequenceNumberToSaveUnder);

            return aggregateRoot;
        }
    }
}
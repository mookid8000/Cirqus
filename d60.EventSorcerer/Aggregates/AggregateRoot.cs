using System;
using System.Linq;
using d60.EventSorcerer.Events;
using d60.EventSorcerer.Extensions;
using d60.EventSorcerer.Numbers;

namespace d60.EventSorcerer.Aggregates
{
    public abstract class AggregateRoot
    {
        internal IEventCollector EventCollector { get; set; }
        internal ISequenceNumberGenerator SequenceNumberGenerator { get; set; }
        internal IAggregateRootRepository AggregateRootRepository { get; set; }
        internal void Initialize(Guid id)
        {
            Id = id;
        }

        public Guid Id { get; private set; }

        protected void Emit<TAggregateRoot>(DomainEvent<TAggregateRoot> e) where TAggregateRoot : AggregateRoot
        {
            if (e == null) throw new ArgumentNullException("e", "Can't emit null!");

            if (Id == Guid.Empty)
            {
                throw new InvalidOperationException(
                    string.Format(
                        "Attempted to emit event {0} from aggregate root {1}, but it has not yet been assigned an ID!",
                        e, GetType()));
            }

            var emitterInterface = typeof(IEmit<>).MakeGenericType(e.GetType());
            if (!emitterInterface.IsAssignableFrom(GetType()))
            {
                throw new InvalidOperationException(
                    string.Format(
                        "Attempted to emit event {0} but the aggregate root {1} does not implement IEmit<{2}>",
                        e, GetType().Name, e.GetType().Name));
            }

            var eventType = e.GetType();

            if (EventCollector == null)
            {
                throw new InvalidOperationException(string.Format("Attempted to emit event {0}, but the aggreate root does not have an event collector!", e));
            }

            if (SequenceNumberGenerator == null)
            {
                throw new InvalidOperationException(string.Format("Attempted to emit event {0}, but the aggreate root does not have a sequence number generator!", e));
            }

            if (typeof(TAggregateRoot) != GetType())
            {
                throw new InvalidOperationException(
                    string.Format("Attempted to emit event {0} which is owned by {1} from aggregate root of type {2}",
                        e, typeof(TAggregateRoot), GetType()));
            }

            var now = Time.GetUtcNow();
            var sequenceNumber = SequenceNumberGenerator.Next();

            e.Meta[DomainEvent.MetadataKeys.AggregateRootId] = Id;
            e.Meta[DomainEvent.MetadataKeys.TimeLocal] = now.ToLocalTime();
            e.Meta[DomainEvent.MetadataKeys.TimeUtc] = now;
            e.Meta[DomainEvent.MetadataKeys.SequenceNumber] = sequenceNumber;
            e.Meta[DomainEvent.MetadataKeys.Owner] = GetOwnerFromType(GetType());
            
            e.Meta.TakeFromAttributes(eventType);
            e.Meta.TakeFromAttributes(GetType());

            try
            {
                var dynamicThis = (dynamic)this;

                dynamicThis.Apply((dynamic)e);
            }
            catch (Exception exception)
            {
                throw new ApplicationException(string.Format(@"Could not apply event {0} to {1} - please make sure that the aggregate root type is public and contains an application method with the following signature:

public void Apply({2} e)
{{
    // change aggregate root state in here
}}

", e, this, eventType.Name), exception);
            }

            EventCollector.Add(e);
        }

        internal static string GetOwnerFromType(Type aggregateRootType)
        {
            return aggregateRootType.Name;
        }

        public override string ToString()
        {
            return string.Format("{0} ({1})", GetType().Name, Id);
        }

        internal long GlobalSequenceNumberCutoff = int.MaxValue;

        protected TAggregateRoot Load<TAggregateRoot>(Guid id, bool createIfNotExists = false) where TAggregateRoot : AggregateRoot, new()
        {
            if (AggregateRootRepository == null)
            {
                throw new InvalidOperationException(
                    string.Format(
                        "Attempted to Load {0} with ID {1} from {2}, but it has not been initialize with an aggregate root repository!",
                        typeof (TAggregateRoot), id, GetType()));
            }

            if (!createIfNotExists && !AggregateRootRepository.Exists<TAggregateRoot>(id, maxGlobalSequenceNumber: GlobalSequenceNumberCutoff))
            {
                throw new ArgumentException(string.Format("Aggregate root {0} with ID {1} does not exist!", typeof(TAggregateRoot), id), "id");
            }

            var aggregateRootInfo = AggregateRootRepository.Get<TAggregateRoot>(id, maxGlobalSequenceNumber: GlobalSequenceNumberCutoff);
            var aggregateRoot = aggregateRootInfo.AggregateRoot;
            aggregateRoot.EventCollector = EventCollector;
            aggregateRoot.SequenceNumberGenerator = SequenceNumberGenerator;
            return aggregateRoot;
        }
    }
}
using System;
using System.Collections.Generic;
using d60.Cirqus.Events;
using d60.Cirqus.Extensions;

namespace d60.Cirqus.Aggregates
{
    /// <summary>
    /// Encapsulates information about an aggregate root instance
    /// </summary>
    public abstract class AggregateRootInfo
    {
        public abstract long LastSeqNo { get; }

        public abstract long LastGlobalSeqNo { get; }

        public bool IsNew
        {
            get { return LastSeqNo == AggregateRoot.InitialAggregateRootSequenceNumber; }
        }

        public abstract Type AggregateRootType { get; }

        public abstract Guid AggregateRootId { get; }
    }

    public class AggregateRootInfo<TAggregateRoot> : AggregateRootInfo where TAggregateRoot : AggregateRoot
    {
        public static AggregateRootInfo<TAggregateRoot> Create(TAggregateRoot aggregateRoot)
        {
            return new AggregateRootInfo<TAggregateRoot>(aggregateRoot);
        }

        AggregateRootInfo(TAggregateRoot aggregateRoot)
        {
            AggregateRoot = aggregateRoot;
        }

        public TAggregateRoot AggregateRoot { get; private set; }

        public override long LastSeqNo
        {
            get { return AggregateRoot.CurrentSequenceNumber; }
        }

        public override long LastGlobalSeqNo
        {
            get { return AggregateRoot.GlobalSequenceNumberCutoff; }
        }

        public override Type AggregateRootType
        {
            get { return typeof(TAggregateRoot); }
        }

        public override Guid AggregateRootId
        {
            get { return AggregateRoot.Id; }
        }

        public void Apply(IEnumerable<DomainEvent> eventsToApply, IUnitOfWork unitOfWork)
        {
            AggregateRoot.UnitOfWork = unitOfWork;

            var dynamicAggregate = (dynamic)AggregateRoot;
            var previousCutoff = AggregateRoot.GlobalSequenceNumberCutoff;

            using (new ThrowingUnitOfWork(AggregateRoot))
            {
                AggregateRoot.ReplayState = ReplayState.ReplayApply;

                foreach (var e in eventsToApply)
                {
                    // ensure that other aggregates loaded during event application are historic if that's required
                    AggregateRoot.GlobalSequenceNumberCutoff = e.GetGlobalSequenceNumber();

                    try
                    {
                        var expectedNextSequenceNumber = AggregateRoot.CurrentSequenceNumber + 1;

                        if (expectedNextSequenceNumber != e.GetSequenceNumber())
                        {
                            throw new InvalidOperationException(string.Format("Attempted to apply event {0} to root {1} with ID {2}, but the expected next seq no is {3}!!!",
                                e.GetSequenceNumber(), typeof(TAggregateRoot), AggregateRoot.Id, expectedNextSequenceNumber));
                        }

                        dynamicAggregate.Apply((dynamic)e);
                        AggregateRoot.CurrentSequenceNumber = e.GetSequenceNumber();
                    }
                    catch (Exception exception)
                    {
                        throw new ApplicationException(string.Format("Could not apply event {0} to {1}", e, AggregateRoot), exception);
                    }
                }
            }

            // restore the cutoff so we don't hinder the root's ability to load other aggregate roots from its emitter methods
            //AggregateRoot.GlobalSequenceNumberCutoff = previousCutoff;
            AggregateRoot.ReplayState = ReplayState.None;
        }

        /// <summary>
        /// Sensitive <see cref="IUnitOfWork"/> stub that can be mounted on an aggregate root when it is in a state
        /// where it is NOT allowed to emit events.
        /// </summary>
        class ThrowingUnitOfWork : IUnitOfWork, IDisposable
        {
            readonly AggregateRoot _root;
            readonly IUnitOfWork _originalUnitOfWork;

            public ThrowingUnitOfWork(AggregateRoot root)
            {
                _root = root;
                _originalUnitOfWork = _root.UnitOfWork;
                _root.UnitOfWork = this;
            }

            public void AddEmittedEvent(DomainEvent e)
            {
                throw new InvalidOperationException(string.Format("The aggregate root of type {0} with ID {1} attempted to emit event {2} while applying events, which is not allowed",
                    _root.GetType(), _root.Id, e));
            }

            public TAggregateRoot GetAggregateRootFromCache<TAggregateRoot>(Guid aggregateRootId, long globalSequenceNumberCutoff) where TAggregateRoot : AggregateRoot
            {
                return _originalUnitOfWork.GetAggregateRootFromCache<TAggregateRoot>(aggregateRootId, globalSequenceNumberCutoff);
            }

            public void AddToCache<TAggregateRoot>(TAggregateRoot aggregateRoot, long globalSequenceNumberCutoff) where TAggregateRoot : AggregateRoot
            {
                _originalUnitOfWork.AddToCache(aggregateRoot, globalSequenceNumberCutoff);
            }

            public void Dispose()
            {
                _root.UnitOfWork = _originalUnitOfWork;
            }
        }
    }
}
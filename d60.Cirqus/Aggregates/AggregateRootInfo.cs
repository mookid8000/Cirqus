using System;

namespace d60.Cirqus.Aggregates
{
    public abstract class AggregateRootInfo
    {
        protected AggregateRootInfo(long lastSeqNo, long lastGlobalSeqNo)
        {
            LastSeqNo = lastSeqNo;
            LastGlobalSeqNo = lastGlobalSeqNo;
        }

        public long LastSeqNo { get; private set; }

        public long LastGlobalSeqNo { get; private set; }

        public bool IsNew
        {
            get { return LastSeqNo == -1; }
        }

        public abstract Type AggregateRootType { get; }

        public abstract Guid AggregateRootId { get; }

        //public AggregateRootInfo<TAggregateRoot> GetAsInfoFor<TAggregateRoot>() where TAggregateRoot : AggregateRoot
        //{
        //    try
        //    {
        //        var aggregateRootInfoToReturn = (AggregateRootInfo<TAggregateRoot>)this;

        //        return aggregateRootInfoToReturn;
        //    }
        //    catch (Exception)
        //    {
        //        return null;
        //    }

        //}
    }

    public class AggregateRootInfo<TAggregate> : AggregateRootInfo where TAggregate : AggregateRoot
    {
        public static AggregateRootInfo<TAggregate> New(TAggregate aggregateRoot)
        {
            return new AggregateRootInfo<TAggregate>(aggregateRoot, -1, -1);
        }

        public static AggregateRootInfo<TAggregate> Old(TAggregate aggregateRoot, long lastSeqNo, long lastGlobalSeqNo)
        {
            return new AggregateRootInfo<TAggregate>(aggregateRoot, lastSeqNo, lastGlobalSeqNo);
        }

        AggregateRootInfo(TAggregate aggregateRoot, long lastSeqNo, long lastGlobalSeqNo)
            : base(lastSeqNo, lastGlobalSeqNo)
        {
            AggregateRoot = aggregateRoot;
        }

        public TAggregate AggregateRoot { get; private set; }

        public override Type AggregateRootType
        {
            get { return typeof(TAggregate); }
        }

        public override Guid AggregateRootId
        {
            get { return AggregateRoot.Id; }
        }
    }
}
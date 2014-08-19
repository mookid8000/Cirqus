using System;

namespace d60.Cirqus.TestHelpers
{
    public class AggregateRootTestInfo
    {
        internal AggregateRootTestInfo(Guid id, long seqNo, long globalSeqNo)
        {
            Id = id;
            SeqNo = seqNo;
            GlobalSeqNo = globalSeqNo;
        }

        public Guid Id { get; private set; }

        public long SeqNo { get; private set; }

        public long GlobalSeqNo { get; private set; }

        public override string ToString()
        {
            return string.Format("{0}: {1} ({2})", Id, SeqNo, GlobalSeqNo);
        }
    }
}
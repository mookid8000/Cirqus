namespace d60.Cirqus.RavenDB
{
    public class RavenEvent
    {
        public string Id
        {
            get { return string.Format("{0}/{1}", AggId, SeqNo); }
        }

        public string AggId { get; set; }
        public long SeqNo { get; set; }
        public long GlobSeqNo { get; set; }
        public byte[] Data { get; set; }
        public byte[] Meta { get; set; }
    }
}
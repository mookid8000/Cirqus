namespace d60.Cirqus.RavenDB
{
    public class GlobalSeqNumber
    {
        public string Id { get; private set; }
        public long Max { get; set; }

        public GlobalSeqNumber()
        {
            Id = "globalseqnumber";
        }
    }
}
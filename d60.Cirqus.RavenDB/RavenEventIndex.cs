using System.Linq;
using Raven.Abstractions.Indexing;
using Raven.Client.Indexes;

namespace d60.Cirqus.RavenDB
{
    public class RavenEventIndex : AbstractIndexCreationTask<RavenEvent>
    {
        public RavenEventIndex()
        {
            Map = events => from e in events
                select new
                {
                    e.AggId,
                    e.SeqNo,
                    e.GlobSeqNo
                };

            Sort(x => x.SeqNo, SortOptions.Long);
            Sort(x => x.GlobSeqNo, SortOptions.Long);
        }
    }
}
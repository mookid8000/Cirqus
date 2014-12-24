using System;
using System.Collections.Generic;

namespace d60.Cirqus.RavenDB
{
    public class Batch
    {
        public Guid Id { get; set; }
        public List<string> EventIds { get; set; }

        public Batch()
        {
            EventIds = new List<string>();
        }
    }
}
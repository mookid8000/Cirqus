using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Transactions;
using d60.Cirqus.Events;
using d60.Cirqus.Exceptions;
using d60.Cirqus.Extensions;
using d60.Cirqus.Numbers;
using d60.Cirqus.Serialization;
using Raven.Client;
using Raven.Client.Document;
using Raven.Client.Embedded;
using Raven.Client.Indexes;

namespace d60.Cirqus.RavenDB
{
    public class RavenDBEventStore : IEventStore, IDisposable
    {
        private IDocumentStore _docStore;
        readonly MetadataSerializer _metadataSerializer = new MetadataSerializer();

        public RavenDBEventStore(string connectionStringName, bool runInMemory = false)
        {
            if (string.IsNullOrEmpty(connectionStringName) && runInMemory)
            {
                _docStore = new EmbeddableDocumentStore {RunInMemory = true};
            }
            else if (!string.IsNullOrEmpty(connectionStringName))
            {
                _docStore = new DocumentStore
                {
                    ConnectionStringName = connectionStringName
                };
            }
            else
            {
                throw new ArgumentNullException("connectionStringName");
            }

            _docStore.Initialize();
            IndexCreation.CreateIndexes(GetType().Assembly, _docStore);
        }

        public void Save(Guid batchId, IEnumerable<EventData> batch)
        {
            var eventList = batch.ToList();

            using (var session = _docStore.OpenSession())
            {
                session.Advanced.MaxNumberOfRequestsPerSession = int.MaxValue;
                var nextSequenceNumber = GetNextGlobalSequenceNumber(session);

                foreach (var e in eventList)
                {
                    e.Meta[DomainEvent.MetadataKeys.GlobalSequenceNumber] = (nextSequenceNumber++).ToString(Metadata.NumberCulture);
                    e.Meta[DomainEvent.MetadataKeys.BatchId] = batchId.ToString();
                }

                EventValidation.ValidateBatchIntegrity(batchId, eventList);

                var batchDoc = new Batch {Id = batchId};

                foreach (var e in eventList)
                {
                    var ev = new RavenEvent
                    {
                        AggId = e.GetAggregateRootId(),
                        SeqNo = Convert.ToInt64(e.Meta[DomainEvent.MetadataKeys.SequenceNumber]),
                        GlobSeqNo = Convert.ToInt64(e.Meta[DomainEvent.MetadataKeys.GlobalSequenceNumber]),
                        Data = e.Data,
                        Meta = Encoding.UTF8.GetBytes(_metadataSerializer.Serialize(e.Meta))
                    };

                    if (session.Load<RavenEvent>(ev.Id) != null)
                    {
                        throw new ConcurrencyException(batchId, eventList, null);
                    }

                    session.Store(ev);
                    session.Store(new GlobalSequence
                    {
                        Id = string.Format("globalseq/{0}", e.Meta[DomainEvent.MetadataKeys.GlobalSequenceNumber]),
                        EventId = ev.Id
                    });
                    batchDoc.EventIds.Add(ev.Id);
                }
                SaveGlobalSequenceNumber(session, nextSequenceNumber - 1);
                session.Store(batchDoc);

                session.SaveChanges();
            }
        }

        public IEnumerable<EventData> Load(string aggregateRootId, long firstSeq = 0)
        {
            using (var session = _docStore.OpenSession())
            {
                var q =
                    session.Query<RavenEvent, RavenEventIndex>()
                        .Where(re => re.AggId == aggregateRootId && re.SeqNo >= firstSeq)
                        .OrderBy(re => re.SeqNo);
                using (var stream = session.Advanced.Stream(q))
                {
                    while (stream.MoveNext())
                    {
                        yield return ReadEvent(stream.Current.Document);
                    }
                }
            }
        }

        public IEnumerable<EventData> Stream(long globalSequenceNumber = 0)
        {
            using (var session = _docStore.OpenSession())
            {
                var q =
                    session.Query<RavenEvent, RavenEventIndex>()
                        .Customize(c => c.WaitForNonStaleResults())
                        .Where(re => re.GlobSeqNo >= globalSequenceNumber)
                        .OrderBy(re => re.GlobSeqNo);
                using (var stream = session.Advanced.Stream(q))
                {
                    while (stream.MoveNext())
                    {
                        yield return ReadEvent(stream.Current.Document);
                    }
                }
            }
        }

        EventData ReadEvent(RavenEvent @event)
        {
            var data = @event.Data;
            var meta = @event.Meta;

            return EventData.FromMetadata(_metadataSerializer.Deserialize(Encoding.UTF8.GetString(meta)), data);
        }

        void SaveGlobalSequenceNumber(IDocumentSession session, long globalSequenceNumber)
        {
            var doc = GetNextGlobalSequenceNumberDoc(session) ?? new GlobalSeqNumber();
            doc.Max = globalSequenceNumber;
            session.Store(doc);
        }

        public long GetNextGlobalSequenceNumber()
        {
            using (var session = _docStore.OpenSession())
            {
                return GetNextGlobalSequenceNumber(session);
            }
        }
        
        long GetNextGlobalSequenceNumber(IDocumentSession session)
        {
            var result = GetNextGlobalSequenceNumberDoc(session);
            return result != null
                    ? result.Max + 1
                    : 0;
        }

        GlobalSeqNumber GetNextGlobalSequenceNumberDoc(IDocumentSession session)
        {
            return session.Load<GlobalSeqNumber>(new GlobalSeqNumber().Id);
        }

        public void Dispose()
        {
            if (_docStore != null)
                _docStore.Dispose();
        }
    }
}
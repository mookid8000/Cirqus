using System.Collections.Generic;
using System.Linq;
using d60.EventSorcerer.Events;
using d60.EventSorcerer.Exceptions;
using d60.EventSorcerer.Extensions;
using d60.EventSorcerer.Views.Basic;

namespace d60.EventSorcerer.MongoDb.Views
{
    class MongoDbCatchUpView<TView> : MongoDbView<TView> where TView : IView, ISubscribeTo
    {
        public MongoDbCatchUpView()
        {
            Pointers = new Dictionary<string, int>();
        }
        public Dictionary<string, int> Pointers { get; set; }
        public void DispatchAndResolve(IEventStore eventStore, DomainEvent domainEvent)
        {
            var aggregateRootId = domainEvent.GetAggregateRootId();
            var aggIdString = aggregateRootId.ToString();
            var seqNo = domainEvent.GetSequenceNumber();

            if (!Pointers.ContainsKey(aggIdString))
            {
                Dispatch(domainEvent);
                Pointers[aggIdString] = seqNo;
                return;
            }

            var expectedNextSeqNo = Pointers[aggIdString] + 1;

            while (expectedNextSeqNo != seqNo)
            {
                var missingEvent = eventStore
                    .Load(aggregateRootId, expectedNextSeqNo, 1)
                    .FirstOrDefault();

                if (missingEvent == null) break;

                Dispatch(missingEvent);
                expectedNextSeqNo++;
            }

            if (seqNo != expectedNextSeqNo)
            {
                throw new ConsistencyException(
                    "Cannot handle event {0} for {1} on view {2} because the view expected sequence number {3}",
                    seqNo, aggIdString, Id, expectedNextSeqNo);
            }

            Dispatch(domainEvent);
            Pointers[aggIdString] = seqNo;
        }
    }
}
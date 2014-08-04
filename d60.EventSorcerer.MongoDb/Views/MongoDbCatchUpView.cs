using System.Collections.Generic;
using System.Linq;
using d60.EventSorcerer.Events;
using d60.EventSorcerer.Exceptions;
using d60.EventSorcerer.Extensions;
using d60.EventSorcerer.Views.Basic;

namespace d60.EventSorcerer.MongoDb.Views
{
    class MongoDbCatchUpView<TView> where TView : IView, ISubscribeTo
    {
        protected readonly ViewDispatcherHelper<TView> Dispatcher = new ViewDispatcherHelper<TView>();

        public MongoDbCatchUpView()
        {
            Pointers = new Dictionary<string, long>();
        }
        public Dictionary<string, long> Pointers { get; set; }
        public long MaxGlobalSeq { get; set; }
        public string Id { get; set; }
        public TView View { get; set; }

        public void DispatchAndResolve(IEventStore eventStore, DomainEvent domainEvent, IViewContext context)
        {
            var aggregateRootId = domainEvent.GetAggregateRootId();
            var aggIdString = aggregateRootId.ToString();
            var seqNo = domainEvent.GetSequenceNumber();

            var expectedNextSeqNo = Pointers.ContainsKey(aggIdString)
                ? Pointers[aggIdString] + 1
                : 0;

            if (expectedNextSeqNo > seqNo)
            {
                return;
            }

            while (expectedNextSeqNo < seqNo)
            {
                var missingEvent = eventStore
                    .Load(aggregateRootId, expectedNextSeqNo, 1)
                    .FirstOrDefault();

                if (missingEvent == null) break;

                Dispatcher.DispatchToView(context, missingEvent, View);
            
                expectedNextSeqNo++;
                MaxGlobalSeq = missingEvent.GetGlobalSequenceNumber();
            }

            if (seqNo != expectedNextSeqNo)
            {
                throw new ConsistencyException(
                    "Cannot handle event {0} for {1} on view {2} because the view expected sequence number {3}",
                    seqNo, aggIdString, Id, expectedNextSeqNo);
            }

            Dispatcher.DispatchToView(context, domainEvent, View);

            Pointers[aggIdString] = seqNo;
            MaxGlobalSeq = domainEvent.GetGlobalSequenceNumber();
        }
    }
}
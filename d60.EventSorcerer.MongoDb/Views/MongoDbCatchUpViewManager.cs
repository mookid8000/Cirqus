using System;
using System.Collections.Generic;
using System.Linq;
using d60.EventSorcerer.Events;
using d60.EventSorcerer.Exceptions;
using d60.EventSorcerer.Extensions;
using d60.EventSorcerer.Views.Basic;
using MongoDB.Driver;
using MongoDB.Driver.Builders;

namespace d60.EventSorcerer.MongoDb.Views
{
    public class MongoDbCatchUpViewManager<TView> : IViewManager where TView : class, IView, ISubscribeTo, new()
    {
        readonly MongoCollection<MongoDbCatchUpView<TView>> _viewCollection;
        int _maxDomainEventsBetweenFlush = 100;

        public MongoDbCatchUpViewManager(MongoDatabase database, string collectionName)
        {
            _viewCollection = database.GetCollection<MongoDbCatchUpView<TView>>(collectionName);
            _viewCollection.CreateIndex(IndexKeys<MongoDbCatchUpView<TView>>.Ascending(v => v.MaxGlobalSeq));
        }

        /// <summary>
        /// Configures how many events are dispatched to view instances between
        /// </summary>
        public int MaxDomainEventsBetweenFlush
        {
            get { return _maxDomainEventsBetweenFlush; }
            set
            {
                if (value <= 0)
                {
                    throw new ArgumentException(string.Format("Attempted to set MaxDomainEventsBetweenFlush to {0}, but it must be greater than 0!", value));
                }
                _maxDomainEventsBetweenFlush = value;
            }
        }

        public void Initialize(IEventStore eventStore, bool purgeExisting = false)
        {
            if (purgeExisting)
            {
                Purge();
            }

            var viewInstanceWithMaxGlobalSequenceNumber = _viewCollection
                .FindAllAs<MongoDbCatchUpView<TView>>()
                .SetSortOrder(SortBy<MongoDbCatchUpView<TView>>.Descending(v => v.MaxGlobalSeq))
                .SetLimit(1)
                .FirstOrDefault();

            var globalSequenceNumberCutoff = viewInstanceWithMaxGlobalSequenceNumber == null
                ? 0
                : viewInstanceWithMaxGlobalSequenceNumber.MaxGlobalSeq + 1;

            var batches = eventStore.Stream(globalSequenceNumberCutoff).Batch(1000);

            foreach (var partition in batches)
            {
                Dispatch(eventStore, partition);
            }
        }

        public void Purge()
        {
            _viewCollection.Drop();
        }

        public void Dispatch(IEventStore eventStore, IEnumerable<DomainEvent> events)
        {
            var eventsList = events.ToList();

            try
            {
                foreach (var batch in eventsList.Batch(MaxDomainEventsBetweenFlush))
                {
                    ProcessOneBatch(eventStore, batch);
                }
            }
            catch
            {
                try
                {
                    // make sure we flush after each single domain event
                    foreach (var e in eventsList)
                    {
                        ProcessOneBatch(eventStore, new[] {e});
                    }
                }
                catch (ConsistencyException)
                {
                    throw;
                }
                catch
                {
                }
            }
        }

        void ProcessOneBatch(IEventStore eventStore, IEnumerable<DomainEvent> batch)
        {
            var locator = ViewLocator.GetLocatorFor<TView>();
            var activeViewDocsByid = new Dictionary<string, MongoDbCatchUpView<TView>>();

            foreach (var e in batch)
            {
                var viewId = locator.GetViewId(e);
                var doc = activeViewDocsByid
                    .GetOrAdd(viewId, id => _viewCollection.FindOneById(id)
                                            ?? new MongoDbCatchUpView<TView>
                                            {
                                                Id = id,
                                                View = new TView(),
                                            });

                doc.DispatchAndResolve(eventStore, e);
            }

            Save(activeViewDocsByid.Values);
        }

        void Save(IEnumerable<MongoDbCatchUpView<TView>> activeViewDocs)
        {
            foreach (var doc in activeViewDocs)
            {
                _viewCollection.Save(doc);
            }
        }

        public TView Load(string viewId)
        {
            var doc = _viewCollection.FindOneById(viewId);

            return doc != null
                ? doc.View
                : null;
        }
    }
}
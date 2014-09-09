using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Validation;
using System.Linq;
using d60.Cirqus.Events;
using d60.Cirqus.Exceptions;
using d60.Cirqus.Extensions;
using d60.Cirqus.Views.ViewManagers;
using d60.Cirqus.Views.ViewManagers.Old;

namespace d60.Cirqus.EntityFramework
{
    public class EntityFrameworkViewManager<TViewInstance> : IPushViewManager, IPullViewManager where TViewInstance : class, IViewInstance, ISubscribeTo, new()
    {
        readonly ViewDispatcherHelper<TViewInstance> _dispatcherHelper = new ViewDispatcherHelper<TViewInstance>();
        readonly string _connectionStringOrName;

        int _maxDomainEventsBetweenFlush = 100;
        bool _initialized;

        public EntityFrameworkViewManager(string connectionStringOrName, bool createDatabaseIfnotExist = true)
        {
            _connectionStringOrName = connectionStringOrName;

            Database.SetInitializer(new CreateDatabaseIfNotExists<GenericViewContext<TViewInstance>>());

            if (createDatabaseIfnotExist)
            {
                using (var context = new GenericViewContext<TViewInstance>(_connectionStringOrName))
                {
                    //touch tables to create them
                    context.Database.Initialize(true);

                    var tableName = context.ViewTableName;
                    var indexName = "IDX_" + tableName + "_LastGlobalSequenceNumber";

                    context.Database.ExecuteSqlCommand(string.Format(@"

IF NOT EXISTS(SELECT * FROM sys.indexes WHERE name = '{0}' AND object_id = OBJECT_ID('{1}'))
BEGIN
	CREATE NONCLUSTERED INDEX [{0}] ON [dbo].[{1}]
	(
		[LastGlobalSequenceNumber] ASC
	)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)
END

", indexName, tableName));
                }
            }
        }

        public int MaxDomainEventsBetweenFlush
        {
            get { return _maxDomainEventsBetweenFlush; }
            set
            {
                if (value <= 0)
                {
                    throw new ArgumentException(string.Format("Attempted to set max events between flush to {0}, but it must be greater than 0!", value));
                }
                _maxDomainEventsBetweenFlush = value;
            }
        }

        public void Initialize(IViewContext context, IEventStore eventStore, bool purgeExistingViews = false)
        {
            if (purgeExistingViews)
            {
                PurgeViews();
            }

            CatchUp(context, eventStore, long.MaxValue);

            _initialized = true;
        }

        public void CatchUp(IViewContext context, IEventStore eventStore, long lastGlobalSequenceNumber)
        {
            var lastSeenMaxGlobalSequenceNumber = FindMax();

            foreach (var batch in eventStore.Stream(lastSeenMaxGlobalSequenceNumber + 1).Batch(MaxDomainEventsBetweenFlush))
            {
                var eventsList = batch.ToList();
                try
                {
                    using (var genericViewBasse = new GenericViewContext<TViewInstance>(_connectionStringOrName))
                    {
                        foreach (var e in eventsList)
                        {
                            if (!ViewLocator.IsRelevant<TViewInstance>(e)) continue;

                            DispatchEvent(e, genericViewBasse, context);
                        }

                        SaveChanges(genericViewBasse);
                    }
                }
                catch (Exception ex)
                {
                    RetryEvents(context, ex, eventsList);
                }
            }
        }

        public bool Stopped { get; set; }

        void PurgeViews()
        {
            using (var dbContext = new GenericViewContext<TViewInstance>(_connectionStringOrName))
            {
                var sql = "DELETE FROM " + dbContext.ViewTableName;

                dbContext.Database.ExecuteSqlCommand(sql);
                dbContext.SaveChanges();
            }
        }

        public void Dispatch(IViewContext context, IEventStore eventStore, IEnumerable<DomainEvent> events)
        {
            if (!_initialized)
            {
                var message =
                    string.Format("The view manager for {0} has not been initialized! Please make sure that the view" +
                                  " manager is properly initialized, either by initializing it manually, or by having" +
                                  " the event dispatcher do it (which is the preferred way when you\'re working with" +
                                  " an event dispatcher)",
                        typeof(TViewInstance));

                throw new InvalidOperationException(message);
            }

            var lastSeenGlobalSequenceNumber = FindMax();

            var eventsList = events
                .Where(e => e.GetGlobalSequenceNumber() > lastSeenGlobalSequenceNumber)
                .ToList();

            if (!eventsList.Any()) return;

            try
            {
                using (var genericViewBasse = new GenericViewContext<TViewInstance>(_connectionStringOrName))
                {
                    foreach (var e in eventsList)
                    {
                        if (!ViewLocator.IsRelevant<TViewInstance>(e)) continue;

                        DispatchEvent(e, genericViewBasse, context);
                    }

                    SaveChanges(genericViewBasse);
                }
            }
            catch (Exception ex)
            {
                RetryEvents(context, ex, eventsList);
            }
        }

        void RetryEvents(IViewContext context, Exception ex, List<DomainEvent> eventsList)
        {
            Console.WriteLine(ex);
            try
            {
                // make sure we flush after each single domain event
                foreach (var e in eventsList)
                {
                    using (var innerContext = new GenericViewContext<TViewInstance>(_connectionStringOrName))
                    {
                        DispatchEvent(e, innerContext, context);

                        SaveChanges(innerContext);
                    }
                }
            }
            catch (ConsistencyException)
            {
                throw;
            }
            catch (Exception)
            {
            }
        }

        static void SaveChanges(GenericViewContext<TViewInstance> genericViewBasse)
        {
            try
            {
                genericViewBasse.SaveChanges();
            }
            catch (DbEntityValidationException entityValidationException)
            {
                foreach (var error in entityValidationException.EntityValidationErrors)
                {
                    Console.WriteLine("entry:" + error.Entry);

                    foreach (var valError in error.ValidationErrors)
                    {
                        Console.WriteLine("Property: {0} has error: {1}", valError.PropertyName, valError.ErrorMessage);
                    }
                }
            }
        }

        void DispatchEvent(DomainEvent domainEvent, GenericViewContext<TViewInstance> genericViewBasse, IViewContext context)
        {
            var locator = ViewLocator.GetLocatorFor<TViewInstance>();
            var viewIds = locator.GetViewIds(domainEvent);

            foreach (var viewId in viewIds)
            {
                var instance = genericViewBasse.ViewCollection.Find(viewId)
                               ?? CreateAndAddNewViewInstance(genericViewBasse, viewId);

                _dispatcherHelper.DispatchToView(context, domainEvent, instance);
            }
        }

        TViewInstance CreateAndAddNewViewInstance(GenericViewContext<TViewInstance> genericViewBasse, string viewId)
        {
            var instance = new TViewInstance
            {
                Id = viewId, 
                LastGlobalSequenceNumber = -1
            };
            
            genericViewBasse.ViewCollection.Add(instance);
            
            return instance;
        }

        long FindMax()
        {
            using (var context = new GenericViewContext<TViewInstance>(_connectionStringOrName))
            {
                return context.ViewCollection.Any()
                    ? context.ViewCollection.Max(v => v.LastGlobalSequenceNumber)
                    : -1;
            }
        }

        class GenericViewContext<TView> : DbContext where TView : class, IViewInstance
        {
            public GenericViewContext(string connectionstringOrName)
                : base(connectionstringOrName)
            {
            }

            public DbSet<TView> ViewCollection { get; set; }

            public readonly string ViewTableName = typeof(TView).Name;

            protected override void OnModelCreating(DbModelBuilder modelBuilder)
            {
                modelBuilder.Entity<TView>().ToTable(ViewTableName);
                modelBuilder.Entity<TView>().HasKey(v => v.Id);

                base.OnModelCreating(modelBuilder);
            }
        }

        public TViewInstance Load(string id)
        {
            using (var context = new GenericViewContext<TViewInstance>(_connectionStringOrName))
            {
                return context.ViewCollection.AsNoTracking().FirstOrDefault(v => v.Id == id);
            }
        }

        public ILinqContext<TViewInstance> Linq()
        {
            return new DisposableLinqContext<TViewInstance>(new GenericViewContext<TViewInstance>(_connectionStringOrName));
        }

        class DisposableLinqContext<TVIew> : ILinqContext<TVIew> where TVIew : class, IViewInstance
        {
            readonly GenericViewContext<TVIew> _context;

            public DisposableLinqContext(GenericViewContext<TVIew> context)
            {
                _context = context;
            }

            public IQueryable<TVIew> Query()
            {
                return _context.ViewCollection;
            }

            public void Dispose()
            {
                _context.Dispose();
            }
        }
    }

    public interface ILinqContext<TView> : IDisposable
    {
        IQueryable<TView> Query();
    }
}

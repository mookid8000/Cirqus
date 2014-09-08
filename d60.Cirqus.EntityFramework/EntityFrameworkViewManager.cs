using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.Migrations;
using System.Linq;
using d60.Cirqus.Events;
using d60.Cirqus.Exceptions;
using d60.Cirqus.Extensions;
using d60.Cirqus.Views.ViewManagers;
using d60.Cirqus.Views.ViewManagers.Old;

namespace d60.Cirqus.EntityFramework
{
    public class EntityFrameworkViewManager<TView> : IPushViewManager, IPullViewManager where TView : class, IViewInstance, ISubscribeTo, new()
    {
        readonly ViewDispatcherHelper<TView> _dispatcherHelper = new ViewDispatcherHelper<TView>();
        readonly string _connectionStringOrName;
        int _maxDomainEventsBetweenFlush = 100;
        bool _initialized;

        public EntityFrameworkViewManager(string connectionStringOrName, bool createDatabaseIfnotExist = true)
        {
            _connectionStringOrName = connectionStringOrName;

            Database.SetInitializer(new CreateDatabaseIfNotExists<GenericViewContext<TView>>());
            //DbConfiguration.Loaded += (o, ea) => ea.ReplaceService<IDbContextFactory<GenericViewContext<TView>>>((s,k) => new Factory<TView>(_connectionStringOrName));
            //Database.SetInitializer(new MigrateDatabaseToLatestVersion<GenericViewContext<TView>, GenericMigrationsThingie>());

            if (createDatabaseIfnotExist)
            {
                using (var context = new GenericViewContext<TView>(_connectionStringOrName))
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

        class Factory<TView> : IDbContextFactory<GenericViewContext<TView>> where TView : class, IViewInstance
        {
            readonly string _connectionString;

            public Factory(string connectionString)
            {
                Console.WriteLine("Whoaaaa, creating the factory!!!");
                _connectionString = connectionString;
            }

            public GenericViewContext<TView> Create()
            {
                return new GenericViewContext<TView>(_connectionString);
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
                    using (var genericViewBasse = new GenericViewContext<TView>(_connectionStringOrName))
                    {
                        foreach (var e in eventsList)
                        {
                            if (!ViewLocator.IsRelevant<TView>(e)) continue;

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
            using (var dbContext = new GenericViewContext<TView>(_connectionStringOrName))
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
                        typeof(TView));

                throw new InvalidOperationException(message);
            }

            var lastSeenGlobalSequenceNumber = FindMax();

            var eventsList = events
                .Where(e => e.GetGlobalSequenceNumber() > lastSeenGlobalSequenceNumber)
                .ToList();

            if (!eventsList.Any()) return;

            try
            {
                using (var genericViewBasse = new GenericViewContext<TView>(_connectionStringOrName))
                {
                    foreach (var e in eventsList)
                    {
                        if (!ViewLocator.IsRelevant<TView>(e)) continue;

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
                    using (var innerContext = new GenericViewContext<TView>(_connectionStringOrName))
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

        static void SaveChanges(GenericViewContext<TView> genericViewBasse)
        {
            try
            {
                genericViewBasse.SaveChanges();
            }
            catch (System.Data.Entity.Validation.DbEntityValidationException entityValidationException)
            {
                foreach (var error in entityValidationException.EntityValidationErrors)
                {
                    Console.WriteLine("entry:" + error.Entry);

                    foreach (var valError in error.ValidationErrors)
                    {
                        Console.WriteLine(String.Format("Property: {0} has error: {1}", valError.PropertyName, valError.ErrorMessage));
                    }
                }
            }
            //catch (System.Data.Entity.Infrastructure.DbUpdateException updateException)
            //{
            //    foreach (var dbEntityEntry in updateException.Entries)
            //    {
            //        Console.WriteLine(dbEntityEntry.Entity.GetType().Name);
            //    }

            //    throw;
            //}
        }

        void DispatchEvent(DomainEvent domainEvent, GenericViewContext<TView> genericViewBasse, IViewContext context)
        {
            var locator = ViewLocator.GetLocatorFor<TView>();
            var ids = locator.GetViewIds(domainEvent);

            foreach (var id in ids)
            {
                var instance = genericViewBasse.ViewCollection.Find(id)
                               ?? CreateAndAddNewViewInstance(genericViewBasse, id);

                var globalSequenceNumber = domainEvent.GetGlobalSequenceNumber();

                if (globalSequenceNumber < instance.LastGlobalSequenceNumber) return;

                _dispatcherHelper.DispatchToView(context, domainEvent, instance);
            }
        }

        TView CreateAndAddNewViewInstance(GenericViewContext<TView> genericViewBasse, string id)
        {
            var instance = new TView {Id = id, LastGlobalSequenceNumber = -1};
            genericViewBasse.ViewCollection.Add(instance);
            return instance;
        }

        long FindMax()
        {
            using (var context = new GenericViewContext<TView>(_connectionStringOrName))
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

        class GenericMigrationsThingie : DbMigrationsConfiguration<GenericViewContext<TView>>
        {
            public GenericMigrationsThingie()
            {
                AutomaticMigrationsEnabled = true;
            }
        }

        public TView Load(string id)
        {
            using (var context = new GenericViewContext<TView>(_connectionStringOrName))
            {
                return context.ViewCollection.AsNoTracking().FirstOrDefault(v => v.Id == id);
            }
        }

        public ILinqContext<TView> Linq()
        {
            return new DisposableLinqContext<TView>(new GenericViewContext<TView>(_connectionStringOrName));
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

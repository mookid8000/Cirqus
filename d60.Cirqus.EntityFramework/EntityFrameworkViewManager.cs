using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using d60.Cirqus.Events;
using d60.Cirqus.Extensions;
using d60.Cirqus.Views.ViewManagers;
using d60.Cirqus.Views.ViewManagers.Locators;

namespace d60.Cirqus.EntityFramework
{
    public class EntityFrameworkViewManager<TViewInstance> : AbstractViewManager<TViewInstance> where TViewInstance : class, IViewInstance, ISubscribeTo, new()
    {
        readonly ViewDispatcherHelper<TViewInstance> _dispatcherHelper = new ViewDispatcherHelper<TViewInstance>();
        readonly ViewLocator _viewLocator = ViewLocator.GetLocatorFor<TViewInstance>();
        readonly string _connectionString;

        public EntityFrameworkViewManager(string connectionString, bool createDatabaseIfnotExist = true)
        {
            _connectionString = connectionString;

            Database.SetInitializer(new CreateDatabaseIfNotExists<GenericViewContext<TViewInstance>>());

            if (createDatabaseIfnotExist)
            {
                using (var context = GetContext())
                {
                    //touch tables to create them
                    context.Database.Initialize(true);
                }
            }
        }

        public override long GetPosition(bool canGetFromCache = true)
        {
            using (var context = GetContext())
            {
                var currentPositionRow = context.PositionCollection.FirstOrDefault(p => p.Id == typeof(TViewInstance).Name);

                return currentPositionRow != null
                    ? currentPositionRow.CurrentPosition
                    : -1;
            }
        }

        public override void Dispatch(IViewContext viewContext, IEnumerable<DomainEvent> batch)
        {
            var eventList = batch.ToList();

            if (!eventList.Any()) return;

            var activeViewInstances = new Dictionary<string, TViewInstance>();

            using (var context = GetContext())
            {
                foreach (var e in eventList)
                {
                    if (!ViewLocator.IsRelevant<TViewInstance>(e)) continue;

                    var viewIds = _viewLocator.GetAffectedViewIds(viewContext, e);

                    foreach (var viewId in viewIds)
                    {
                        var viewInstance = activeViewInstances.ContainsKey(viewId)
                            ? activeViewInstances[viewId]
                            : (activeViewInstances[viewId] = context.ViewCollection.FirstOrDefault(v => v.Id == viewId)
                                                             ?? CreateAndAddNewViewInstance(viewId, context.ViewCollection));

                        _dispatcherHelper.DispatchToView(viewContext, e, viewInstance);
                    }
                }

                UpdatePersistentCache(context, eventList.Max(e => e.GetGlobalSequenceNumber()));

                context.SaveChanges();
            }

            RaiseUpdatedEventFor(activeViewInstances.Values);
        }

        void UpdatePersistentCache(GenericViewContext<TViewInstance> context, long newCurrentPosition)
        {
            var currentPositionRow = context.PositionCollection
                .FirstOrDefault(p => p.Id == typeof(TViewInstance).Name);

            if (currentPositionRow == null)
            {
                currentPositionRow = new Position { Id = typeof(TViewInstance).Name };
                context.PositionCollection.Add(currentPositionRow);
            }

            currentPositionRow.CurrentPosition = newCurrentPosition;
        }

        TViewInstance CreateAndAddNewViewInstance(string viewId, IDbSet<TViewInstance> viewCollection)
        {
            var viewInstance = _dispatcherHelper.CreateNewInstance(viewId);
            viewCollection.Add(viewInstance);
            return viewInstance;
        }

        public override void Purge()
        {
            using (var context = GetContext())
            {
                using (var tx = context.Database.BeginTransaction())
                {
                    context.Database.ExecuteSqlCommand(string.Format("delete from [{0}]", typeof(TViewInstance).Name));
                    context.Database.ExecuteSqlCommand(string.Format("delete from [{0}]", typeof(TViewInstance).Name + "_Position"));

                    tx.Commit();
                }
            }
        }

        public override TViewInstance Load(string viewId)
        {
            using (var context = GetContext())
            {
                return context.ViewCollection.FirstOrDefault(v => v.Id == viewId);
            }
        }

        GenericViewContext<TViewInstance> GetContext()
        {
            return new GenericViewContext<TViewInstance>(_connectionString);
        }

        class GenericViewContext<TEntity> : DbContext where TEntity : class, IViewInstance
        {
            public GenericViewContext(string connectionStringOrConnectionStringName)
                : base(connectionStringOrConnectionStringName)
            {
            }

            public DbSet<TEntity> ViewCollection { get; set; }

            public DbSet<Position> PositionCollection { get; set; }

            protected override void OnModelCreating(DbModelBuilder modelBuilder)
            {
                modelBuilder.Entity<TEntity>().ToTable(typeof(TEntity).Name);
                modelBuilder.Entity<TEntity>().HasKey(v => v.Id);

                modelBuilder.Entity<Position>().ToTable(typeof(TEntity).Name + "_Position");
                modelBuilder.Entity<Position>().HasKey(v => v.Id);

                base.OnModelCreating(modelBuilder);
            }
        }

        public IDbContext<TViewInstance> CreateContext()
        {
            return new QueryHelper<TViewInstance>(GetContext());
        }

        class QueryHelper<TViewInstance> : IDbContext<TViewInstance> where TViewInstance : class, IViewInstance
        {
            readonly GenericViewContext<TViewInstance> _context;

            public QueryHelper(GenericViewContext<TViewInstance> context)
            {
                _context = context;
            }

            public IQueryable<TViewInstance> Views
            {
                get { return _context.ViewCollection; }
            }

            public void Dispose()
            {
                _context.Dispose();
            }
        }
    }

    public interface IDbContext<TViewInstance> : IDisposable where TViewInstance : class, IViewInstance
    {
        IQueryable<TViewInstance> Views { get; }
    }
}
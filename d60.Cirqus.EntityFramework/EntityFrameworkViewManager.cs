using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
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
            ValidateVirtualCollectionProperties(typeof(TViewInstance));

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

        void ValidateVirtualCollectionProperties(Type type)
        {
            var errors = new List<string>();

            ValidateVirtualCollectionProperties(type, errors);

            if (errors.Any())
            {
                throw new InvalidOperationException(string.Format(@"Cannot create Entity Framework view manager for {0} because of the following issues:
{1}", type.FullName, string.Join(Environment.NewLine, errors.Select(e => "    " + e))));
            }
        }

        readonly HashSet<Type> _genericCollectionTypes = new HashSet<Type>
        {
            typeof (ICollection<>),
            typeof (IList<>),
            typeof (List<>),
        };

        void ValidateVirtualCollectionProperties(Type type, List<string> errors)
        {
            var collectionProperties = type.GetProperties()
                .Select(p => new
                {
                    IsGeneric = p.PropertyType.IsGenericType,
                    Property = p
                })
                .Where(a => a.IsGeneric)
                .Select(a => new
                {
                    GenericTypeDefinition = a.Property.PropertyType.GetGenericTypeDefinition(),
                    Property = a.Property
                })
                .Where(a => _genericCollectionTypes.Contains(a.GenericTypeDefinition))
                .Select(a => new
                {
                    Property = a.Property,
                    ItemType = a.Property.PropertyType.GetGenericArguments()[0]
                })
                .ToList();

            foreach (var collectionProperty in collectionProperties)
            {
                if (!collectionProperty.Property.GetGetMethod().IsVirtual)
                {
                    errors.Add(string.Format("the collection property {0}.{1} must be declared virtual", collectionProperty.Property.DeclaringType, collectionProperty.Property.Name));
                }

                ValidateVirtualCollectionProperties(collectionProperty.ItemType);
            }
        }

        public override string Id
        {
            get { return string.Format("{0}", typeof (TViewInstance).GetPrettyName()); }
        }

        public bool BatchDispatchEnabled { get; set; }

        public override async Task<long> GetPosition(bool canGetFromCache = true)
        {
            using (var context = GetContext())
            {
                var currentPositionRow = context.PositionCollection.FirstOrDefault(p => p.Id == typeof(TViewInstance).Name);

                return currentPositionRow != null
                    ? currentPositionRow.CurrentPosition
                    : -1;
            }
        }

        public override void Dispatch(IViewContext viewContext, IEnumerable<DomainEvent> batch, IViewManagerProfiler viewManagerProfiler)
        {
            var eventList = batch.ToList();

            if (!eventList.Any()) return;


            if (BatchDispatchEnabled)
            {
                var domainEventBatch = new DomainEventBatch(eventList);
                eventList.Clear();
                eventList.Add(domainEventBatch);
            }
            var activeViewInstances = new Dictionary<string, TViewInstance>();

            using (var context = GetContext())
            {
                foreach (var e in eventList)
                {
                    if (!ViewLocator.IsRelevant<TViewInstance>(e)) continue;

                    var stopwatch = Stopwatch.StartNew();
                    var viewIds = _viewLocator.GetAffectedViewIds(viewContext, e);

                    foreach (var viewId in viewIds)
                    {
                        var viewInstance = activeViewInstances.ContainsKey(viewId)
                            ? activeViewInstances[viewId]
                            : (activeViewInstances[viewId] = context.ViewCollection.FirstOrDefault(v => v.Id == viewId)
                                                             ?? CreateAndAddNewViewInstance(viewId, context.ViewCollection));

                        _dispatcherHelper.DispatchToView(viewContext, e, viewInstance);
                    }

                    viewManagerProfiler.RegisterTimeSpent(this, e, stopwatch.Elapsed);
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
                    // accept the slowness for now... think of better way of flushing the view, including any tables with FKs
                    foreach (var instance in context.ViewCollection)
                    {
                        context.ViewCollection.Remove(instance);
                    }
                    //context.Database.ExecuteSqlCommand(string.Format("delete from [{0}]", typeof(TViewInstance).Name));
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

        public override void Delete(string viewId)
        {
            
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

            public override int SaveChanges()
            {
                return base.SaveChanges();
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
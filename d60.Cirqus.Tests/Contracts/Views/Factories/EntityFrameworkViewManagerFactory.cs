using System.Collections.Generic;
using System.Linq;
using d60.Cirqus.EntityFramework;
using d60.Cirqus.Tests.Contracts.Views.Models.ObjectGraph;
using d60.Cirqus.Tests.MsSql;
using d60.Cirqus.Views;
using d60.Cirqus.Views.ViewManagers;

namespace d60.Cirqus.Tests.Contracts.Views.Factories
{
    public class EntityFrameworkViewManagerFactory : AbstractViewManagerFactory
    {
        readonly List<IViewManager> _createdEntityFrameworkViewManagers = new List<IViewManager>();
        readonly string _connectionString;

        public EntityFrameworkViewManagerFactory()
        {
            MsSqlTestHelper.EnsureTestDatabaseExists();

            MsSqlTestHelper.DropTable("__MigrationHistory");

            _connectionString = MsSqlTestHelper.ConnectionString;
        }

        protected override IViewManager<TViewInstance> CreateViewManager<TViewInstance>()
        {
            var tableName = typeof(TViewInstance).Name;

            if (typeof(TViewInstance) == typeof(ViewRoot))
            {
                MsSqlTestHelper.DropTable("ViewChilds");
            }

            MsSqlTestHelper.DropTable(tableName);
            MsSqlTestHelper.DropTable(tableName + "_Position");

            var viewManager = new EntityFrameworkViewManager<TViewInstance>(_connectionString);

            _createdEntityFrameworkViewManagers.Add(viewManager);

            return viewManager;
        }

        public override TViewInstance Load<TViewInstance>(string viewId)
        {
            var viewManager = _createdEntityFrameworkViewManagers
                .OfType<EntityFrameworkViewManager<TViewInstance>>()
                .FirstOrDefault();

            if (viewManager == null) return null;

            var linqContext = viewManager.CreateContext();
            
            RegisterDisposable(linqContext);

            return linqContext.Views.FirstOrDefault(v => v.Id == viewId);
        }
    }
}
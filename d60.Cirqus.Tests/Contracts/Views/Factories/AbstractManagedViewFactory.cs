using System.Collections.Generic;
using System.Linq;
using d60.Cirqus.Views;
using d60.Cirqus.Views.ViewManagers;

namespace d60.Cirqus.Tests.Contracts.Views.Factories
{
    public abstract class AbstractManagedViewFactory
    {
        readonly List<IViewManager> _managedViews = new List<IViewManager>();

        public TViewInstance Load<TViewInstance>(string viewId) where TViewInstance : class, IViewInstance, ISubscribeTo, new()
        {
            var managedView = GetManagedView<TViewInstance>();

            return managedView.Load(viewId);
        }

        public void PurgeView<TViewInstance>() where TViewInstance : class, IViewInstance, ISubscribeTo, new()
        {
            var managedView = GetManagedView<TViewInstance>();

            managedView.Purge();
        }

        public IViewManager<TViewInstance> GetManagedView<TViewInstance>() where TViewInstance : class, IViewInstance, ISubscribeTo, new()
        {
            var managedView = _managedViews
                .OfType<IViewManager<TViewInstance>>()
                .FirstOrDefault();

            if (managedView == null)
            {
                managedView = CreateManagedView<TViewInstance>();
                managedView.Purge();
                _managedViews.Add(managedView);
            }

            return managedView;
        }

        protected abstract IViewManager<TViewInstance> CreateManagedView<TViewInstance>() where TViewInstance : class, IViewInstance, ISubscribeTo, new();
    }
}
using System.Collections.Generic;
using System.Linq;
using d60.Cirqus.Views;
using d60.Cirqus.Views.ViewManagers;

namespace d60.Cirqus.Tests.Contracts.Views.Factories
{
    public abstract class AbstractViewManagerFactory
    {
        readonly List<IViewManager> _viewManagers = new List<IViewManager>();

        public TViewInstance Load<TViewInstance>(string viewId) where TViewInstance : class, IViewInstance, ISubscribeTo, new()
        {
            var viewManager = GetViewManager<TViewInstance>();

            return viewManager.Load(viewId);
        }

        public void PurgeView<TViewInstance>() where TViewInstance : class, IViewInstance, ISubscribeTo, new()
        {
            var viewManager = GetViewManager<TViewInstance>();

            viewManager.Purge();
        }

        public IViewManager<TViewInstance> GetViewManager<TViewInstance>() where TViewInstance : class, IViewInstance, ISubscribeTo, new()
        {
            var viewManager = _viewManagers
                .OfType<IViewManager<TViewInstance>>()
                .FirstOrDefault();

            if (viewManager == null)
            {
                viewManager = CreateViewManager<TViewInstance>();
                viewManager.Purge();
                _viewManagers.Add(viewManager);
            }

            return viewManager;
        }

        protected abstract IViewManager<TViewInstance> CreateViewManager<TViewInstance>() where TViewInstance : class, IViewInstance, ISubscribeTo, new();
    }
}
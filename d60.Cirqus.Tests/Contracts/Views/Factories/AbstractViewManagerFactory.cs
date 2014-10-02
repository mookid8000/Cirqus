using System;
using System.Collections.Generic;
using System.Linq;
using d60.Cirqus.Views;
using d60.Cirqus.Views.ViewManagers;

namespace d60.Cirqus.Tests.Contracts.Views.Factories
{
    public abstract class AbstractViewManagerFactory : IDisposable
    {
        readonly List<IViewManager> _viewManagers = new List<IViewManager>();
        readonly List<IDisposable> _stuffToDispose = new List<IDisposable>();

        public virtual TViewInstance Load<TViewInstance>(string viewId) where TViewInstance : class, IViewInstance, ISubscribeTo, new()
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
        
        public void Dispose()
        {
            foreach (var disposable in _stuffToDispose)
            {
                Console.WriteLine("Disposing {0}", disposable);
                disposable.Dispose();
            }

            _stuffToDispose.Clear();
        }

        protected void RegisterDisposable(IDisposable disposable)
        {
            _stuffToDispose.Add(disposable);
        }
    }
}
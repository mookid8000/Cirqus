using System;
using System.Collections.Generic;
using System.Linq;
using d60.Cirqus.Logging;

namespace d60.Cirqus.Config.Configurers
{
    public class ResolutionContext : IDisposable
    {
        static Logger _logger;

        static ResolutionContext()
        {
            CirqusLoggerFactory.Changed += f => _logger = f.GetCurrentClassLogger();
        }

        readonly IEnumerable<Resolver> _resolvers;
        readonly Dictionary<Type, int> _levels = new Dictionary<Type, int>();
        readonly Dictionary<Type, object> _cache = new Dictionary<Type, object>();
        readonly HashSet<object> _resolvedObjects = new HashSet<object>();
        readonly List<ResolutionContext> _childContexts = new List<ResolutionContext>();

        public ResolutionContext(IEnumerable<Resolver> resolvers)
        {
            _resolvers = resolvers;
        }

        public TService Get<TService>()
        {
            var result = GetOrDefault<TService>();

            if (Equals(result, default(TService)))
            {
                throw new ResolutionException(typeof(TService), "No appropriate factory method has been registered!");
            }

            return result;
        }

        public TService GetOrDefault<TService>()
        {
            if (_cache.ContainsKey(typeof(TService)))
            {
                var cachedResult = (TService)_cache[typeof(TService)];

                return cachedResult;
            }

            var resolver = _resolvers
                .OfType<Resolver<TService>>()
                .Skip(GetLevelFor<TService>())
                .FirstOrDefault();

            if (resolver == null)
            {
                return default(TService);
            }
            
            AddToLevel<TService>(1);

            var result = resolver.InvokeFactory(this);

            _cache[typeof(TService)] = result;

            _resolvedObjects.Add(result);

            AddToLevel<TService>(-1);

            return result;
        }

        public IEnumerable<TService> GetAll<TService>()
        {
            return _resolvers
                .OfType<Resolver<TService>>()
                .Where(r => r.Multi)
                .Select(r => r.InvokeFactory(this));
        }

        public void AddChildContext(ResolutionContext context)
        {
            _childContexts.Add(context);
        }

        public void Dispose()
        {
            foreach (var disposable in _resolvedObjects.OfType<IDisposable>())
            {
                _logger.Debug("Disposing {0}", disposable);

                disposable.Dispose();
            }

            foreach (var context in _childContexts)
            {
                context.Dispose();
            }
        }

        void AddToLevel<TService>(int addend)
        {
            var serviceType = typeof(TService);

            if (!_levels.ContainsKey(serviceType))
                _levels[serviceType] = 0;

            _levels[serviceType] += addend;
        }

        int GetLevelFor<TService>()
        {
            var serviceType = typeof(TService);

            if (!_levels.ContainsKey(serviceType))
                _levels[serviceType] = 0;

            return _levels[serviceType];
        }

        public abstract class Resolver
        {
            protected Resolver(Delegate factory, bool decorator, bool multi)
            {
                Factory = factory;
                Decorator = decorator;
                Multi = multi;
            }

            public Type Type { get; protected set; }

            public Delegate Factory { get; private set; }

            public bool Decorator { get; private set; }

            public bool Multi { get; private set; }
        }

        public class Resolver<TService> : Resolver
        {
            public Resolver(Delegate factory, bool decorator, bool multi)
                : base(factory, decorator, multi)
            {
                Type = typeof(TService);
            }

            public TService InvokeFactory(ResolutionContext resolutionContext)
            {
                return ((Func<ResolutionContext, TService>)Factory)(resolutionContext);
            }
        }
    }
}
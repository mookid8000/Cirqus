using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;

namespace d60.Cirqus.Config.Configurers
{
    public class ResolutionContext
    {
        readonly IEnumerable<Resolver> _resolvers;
        readonly Dictionary<Type, int> _levels = new Dictionary<Type, int>();
        readonly Dictionary<Type, object> _cache = new Dictionary<Type, object>();
        readonly HashSet<object> _resolvedObjects = new HashSet<object>();

        public ResolutionContext(IEnumerable<Resolver> resolvers)
        {
            _resolvers = resolvers;
        }

        public TService Get<TService>()
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
                throw new InvalidOperationException(string.Format("Cannot provide an instance of {0} because an appropriate factory method has not been registered!", typeof(TService)));
            }

            AddToLevel<TService>(1);

            var result = resolver.InvokeFactory(this);

            _cache[typeof(TService)] = result;

            _resolvedObjects.Add(result);

            AddToLevel<TService>(-1);

            return result;

        }

        void AddToLevel<TService>(int addend)
        {
            var serviceType = typeof(TService);

            if (!_levels.ContainsKey(serviceType))
                _levels[serviceType] = 0;

            _levels[serviceType] += addend;
        }

        public IEnumerable<IDisposable> GetDisposables()
        {
            return _resolvedObjects.OfType<IDisposable>();
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
            protected Resolver(Delegate factory, bool decorator)
            {
                Factory = factory;
                Decorator = decorator;
            }

            public Type Type { get; protected set; }

            public Delegate Factory { get; set; }

            public bool Decorator { get; set; }
        }

        public class Resolver<TService> : Resolver
        {
            public Resolver(Delegate factory, bool decorator) : base(factory, decorator)
            {
                Type = typeof (TService);
            }

            public TService InvokeFactory(ResolutionContext resolutionContext)
            {
                return ((Func<ResolutionContext, TService>) Factory)(resolutionContext);
            }
        }
    }
}
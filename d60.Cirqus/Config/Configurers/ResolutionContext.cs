using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Services;

namespace d60.Cirqus.Config.Configurers
{
    public class ResolutionContext
    {
        readonly IEnumerable<Resolver> _resolvers;
        readonly Dictionary<Type, int> _levels = new Dictionary<Type, int>();
        readonly Dictionary<Type, object> _cache = new Dictionary<Type, object>();

        public ResolutionContext(IEnumerable<Resolver> resolvers)
        {
            _resolvers = resolvers;
        }

        public TService Get<TService>()
        {
            if (_cache.ContainsKey(typeof(TService)))
            {
                var cachedResult = (TService)_cache[typeof(TService)];
                Console.WriteLine("Resolved: {0}{1} (from cache)", new String(' ', GetLevelFor<TService>() * 2), cachedResult);
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

            Console.WriteLine("Resolved: {0}{1}", new String(' ', GetLevelFor<TService>() * 2), result);

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

        int GetLevelFor<TService>()
        {
            var serviceType = typeof(TService);

            if (!_levels.ContainsKey(serviceType))
                _levels[serviceType] = 0;

            return _levels[serviceType];
        }

        public abstract class Resolver
        {
            public Type Type { get; set; }

            public Delegate Factory { get; set; }

            public bool Decorator { get; set; }
        }

        public class Resolver<TService> : Resolver
        {
            public TService InvokeFactory(ResolutionContext resolutionContext)
            {
                return ((Func<ResolutionContext, TService>)Factory)(resolutionContext);
            }
        }
    }
}
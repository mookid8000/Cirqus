using System;
using System.Collections.Generic;
using System.Linq;

namespace d60.Cirqus.Config.Configurers
{
    public class ResolutionContext
    {
        readonly IEnumerable<Delegate> _factoryMethods;
        readonly Dictionary<Type, int> _levels = new Dictionary<Type, int>();
        readonly Dictionary<Type, object> _cache = new Dictionary<Type, object>();

        public ResolutionContext(IEnumerable<Delegate> factoryMethods)
        {
            _factoryMethods = factoryMethods;
        }

        public TService Get<TService>()
        {
            if (_cache.ContainsKey(typeof (TService)))
            {
                var cachedResult = (TService)_cache[typeof(TService)];
                Console.WriteLine("Resolved: {0}{1} (from cache)", new String(' ', GetLevelFor<TService>() * 2), cachedResult);
                return cachedResult;
            }

            var matchingFactoryMethod = _factoryMethods
                .OfType<Func<ResolutionContext, TService>>()
                .Skip(GetLevelFor<TService>())
                .FirstOrDefault();

            if (matchingFactoryMethod == null)
            {
                throw new InvalidOperationException(string.Format("Cannot provide an instance of {0} because an appropriate factory method has not been registered!", typeof(TService)));
            }

            AddToLevel<TService>(1);

            var result = matchingFactoryMethod(this);

            _cache[typeof (TService)] = result;

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
            var serviceType = typeof (TService);

            if (!_levels.ContainsKey(serviceType))
                _levels[serviceType] = 0;

            return _levels[serviceType];
        }
    }
}
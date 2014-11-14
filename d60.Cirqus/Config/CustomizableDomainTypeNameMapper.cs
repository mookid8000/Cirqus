using System;
using System.Collections.Concurrent;
using System.Reflection;
using d60.Cirqus.Aggregates;
using d60.Cirqus.Events;

namespace d60.Cirqus.Config
{
    public class CustomizableDomainTypeNameMapper : IDomainTypeNameMapper
    {
        readonly ConcurrentDictionary<Type, string> _typeToName;
        readonly ConcurrentDictionary<string, Type> _nameToType;

        readonly Func<Type, string> _typeToNameFunction;

        CustomizableDomainTypeNameMapper(Func<Type, string> typeToNameFunction, bool caseSensitive)
        {
            _typeToNameFunction = typeToNameFunction;

            _typeToName = new ConcurrentDictionary<Type, string>();
            _nameToType = new ConcurrentDictionary<string, Type>(caseSensitive ? StringComparer.InvariantCulture : StringComparer.InvariantCultureIgnoreCase);
        }

        /// <summary>
        /// Constructs a case insensitive <see cref="CustomizableDomainTypeNameMapper"/> that uses <see cref="MemberInfo.Name"/> as the default name (i.e.
        /// the short type name without any kind of assembly or namespace names in it)
        /// </summary>
        public static CustomizableDomainTypeNameMapper UseShortTypeNames()
        {
            return new CustomizableDomainTypeNameMapper(type => type.Name, false);
        }

        /// <summary>
        /// Constructs a case sensitive <see cref="CustomizableDomainTypeNameMapper"/> that uses the given function to map each found type to a type name
        /// </summary>
        public static CustomizableDomainTypeNameMapper CustomCaseSensitive(Func<Type, string> typeToNameFunction)
        {
            return new CustomizableDomainTypeNameMapper(typeToNameFunction, true);
        }

        /// <summary>
        /// Constructs a case insensitive <see cref="CustomizableDomainTypeNameMapper"/> that uses the given function to map each found type to a type name
        /// </summary>
        public static CustomizableDomainTypeNameMapper CustomCaseInsensitive(Func<Type, string> typeToNameFunction)
        {
            return new CustomizableDomainTypeNameMapper(typeToNameFunction, false);
        }

        /// <summary>
        /// Scans the given assembly for aggregate root types and domain event types to be added
        /// </summary>
        public CustomizableDomainTypeNameMapper WithTypesFrom(Assembly assembly)
        {
            foreach (var type in assembly.GetTypes())
            {
                PossiblyAddType(type);
            }
            return this;
        }

        /// <summary>
        /// Scans the assembly of the given aggregate root type for aggregate root types and domain event types to be added
        /// </summary>
        public CustomizableDomainTypeNameMapper ScanAssemblyOfAggregateRoot<TAggregateRoot>() where TAggregateRoot : AggregateRoot
        {
            var assembly = typeof(TAggregateRoot).Assembly;
            foreach (var type in assembly.GetTypes())
            {
                PossiblyAddType(type);
            }
            return this;
        }

        /// <summary>
        /// Scans the assembly of the given aggregate root type for aggregate root types and domain event types to be added
        /// </summary>
        public CustomizableDomainTypeNameMapper ScanAssemblyOfDomainEvent<TDomainEvent>() where TDomainEvent : DomainEvent
        {
            var assembly = typeof(TDomainEvent).Assembly;
            foreach (var type in assembly.GetTypes())
            {
                PossiblyAddType(type);
            }
            return this;
        }

        public CustomizableDomainTypeNameMapper AddTypes(params Type[] typesToAdd)
        {
            foreach (var type in typesToAdd)
            {
                PossiblyAddType(type);
            }
            return this;
        }

        public Type GetType(string name)
        {
            try
            {
                return _nameToType[name];
            }
            catch (Exception exception)
            {
                throw new ArgumentException(string.Format("Could not get a .NET type from the name '{0}' - please make sure that the customizable type name mapper is loaded with all the needed aggregate root and domain event types before you start using it", name), exception);
            }
        }

        public string GetName(Type type)
        {
            try
            {
                return _typeToName[type];

            }
            catch (Exception exception)
            {
                throw new ArgumentException(string.Format("Could not get a name from the .NET type {0} - please make sure that the customizable type name mapper is loaded with all the needed aggregate root and domain event types before you start using it", type), exception);
            }
        }

        void PossiblyAddType(Type type)
        {
            if (typeof(AggregateRoot).IsAssignableFrom(type) || typeof(DomainEvent).IsAssignableFrom(type))
            {
                AddType(type);
            }
        }

        void AddType(Type type)
        {
            var name = _typeToNameFunction(type);

            if (_typeToName.ContainsKey(type) && _typeToName[type] != name)
            {
                throw new ArgumentException(string.Format(
                        "Could not add type alias for {0} with name '{1}' because one already exists with the name '{2}'!",
                        type, name, _typeToName[type]));
            }

            if (_nameToType.ContainsKey(name) && _nameToType[name] != type)
            {
                throw new ArgumentException(string.Format(
                        "Could not add type alias with name '{0}' for type {1} because one already exists for the type {2}!",
                        name, type, _nameToType[name]));
            }

            _typeToName[type] = name;
            _nameToType[name] = type;
        }
    }
}
using System;
using System.Collections.Concurrent;
using Newtonsoft.Json.Serialization;

namespace d60.Cirqus.Serialization
{
    public class TypeAliasBinder : DefaultSerializationBinder
    {
        readonly ConcurrentDictionary<Type, string> _typeToName = new ConcurrentDictionary<Type, string>();
        readonly ConcurrentDictionary<string, Type> _nameToType = new ConcurrentDictionary<string, Type>();
        readonly string _specialAssemblyName;

        public TypeAliasBinder(string virtualNamespaceName)
        {
            _specialAssemblyName = virtualNamespaceName;
        }

        public TypeAliasBinder AddType(Type specialType)
        {
            var shortTypeName = specialType.Name;

            if (!_typeToName.ContainsKey(specialType) && _nameToType.ContainsKey(shortTypeName))
            {
                var errorMessage = String.Format("Cannot add short name mapping for {0} because the short type name {1} has already been added for {2}",
                    specialType, shortTypeName, _nameToType[shortTypeName]);

                throw new InvalidOperationException(errorMessage);
            }

            _typeToName[specialType] = shortTypeName;
            _nameToType[shortTypeName] = specialType;

            return this;
        }

        public override void BindToName(Type serializedType, out string assemblyName, out string typeName)
        {
            string customizedTypeName;

            if (_typeToName.TryGetValue(serializedType, out customizedTypeName))
            {
                assemblyName = _specialAssemblyName;
                typeName = customizedTypeName;
                return;
            }

            base.BindToName(serializedType, out assemblyName, out typeName);
        }

        public override Type BindToType(string assemblyName, string typeName)
        {
            Type customizedType;

            if (assemblyName == _specialAssemblyName
                && _nameToType.TryGetValue(typeName, out customizedType))
            {
                return customizedType;
            }

            return base.BindToType(assemblyName, typeName);
        }
    }
}
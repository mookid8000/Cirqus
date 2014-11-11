using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using d60.Cirqus.Commands;
using d60.Cirqus.Numbers;

namespace d60.Cirqus.TsClient.Model
{
    class ProxyGeneratorContext
    {
        readonly Dictionary<Type, TypeDef> _types = new Dictionary<Type, TypeDef>();

        public ProxyGeneratorContext()
            : this(Enumerable.Empty<Type>())
        {
        }

        public ProxyGeneratorContext(IEnumerable<Type> types)
        {
            AddBuiltInType(new BuiltInTypeDef(typeof(bool), "", "boolean"));

            AddBuiltInType(new BuiltInTypeDef(typeof(short), "", "number"));
            AddBuiltInType(new BuiltInTypeDef(typeof(int), "", "number"));
            AddBuiltInType(new BuiltInTypeDef(typeof(long), "", "number"));

            AddBuiltInType(new BuiltInTypeDef(typeof(float), "", "number"));
            AddBuiltInType(new BuiltInTypeDef(typeof(double), "", "number"));
            AddBuiltInType(new BuiltInTypeDef(typeof(decimal), "", "number"));

            AddBuiltInType(new BuiltInTypeDef(typeof(string), "", "string"));
            AddBuiltInType(new BuiltInTypeDef(typeof(DateTime), "", "Date"));

            AddBuiltInType(new BuiltInTypeDef(typeof(object), "", "any"));

            AddBuiltInType(new BuiltInTypeDef(typeof(Command), @"interface Command {
    Meta?: any;
}", "Command"));
            AddBuiltInType(new BuiltInTypeDef(typeof(Metadata), "", "any") { Optional = true });
            AddBuiltInType(new BuiltInTypeDef(typeof(Guid), "interface Guid {}", "Guid"));

            foreach (var type in types)
            {
                AddTypeDefFor(type);
            }
        }

        void AddBuiltInType(BuiltInTypeDef builtInTypeDef)
        {
            _types.Add(builtInTypeDef.Type, builtInTypeDef);
        }

        public void AddTypeDefFor(Type type)
        {
            var qualifiedClassName = new QualifiedClassName(type);

            GetOrCreateTypeDef(qualifiedClassName, type);
        }

        TypeDef GetOrCreateTypeDef(QualifiedClassName qualifiedClassName, Type type)
        {
            return GetExistingTypeDefOrNull(type)
                   ?? CreateSpecialTypeDefOrNull(type)
                   ?? CreateTypeDef(qualifiedClassName, type);
        }

        TypeDef CreateSpecialTypeDefOrNull(Type type)
        {
            BuiltInTypeDef typeDef = null;

            if (typeof(IEnumerable).IsAssignableFrom(type))
            {
                if (type.IsArray && type.GetArrayRank() == 1)
                {
                    var elementType = type.GetElementType();

                    var typeDefOfContainedType = GetOrCreateTypeDef(new QualifiedClassName(elementType), elementType);

                    typeDef = new BuiltInTypeDef(type, "", string.Format("{0}[]", typeDefOfContainedType.FullyQualifiedTsTypeName));
                }
                else if (!type.IsGenericType)
                {
                    typeDef = new BuiltInTypeDef(type, "", "any[]");
                }
                else if (type.IsGenericType && type.GetGenericArguments().Length == 1)
                {
                    var elementType = type.GetGenericArguments()[0];

                    var typeDefOfContainedType = GetOrCreateTypeDef(new QualifiedClassName(elementType), elementType);

                    typeDef = new BuiltInTypeDef(type, "", string.Format("{0}[]", typeDefOfContainedType.FullyQualifiedTsTypeName));
                }
            }

            if (typeDef != null)
            {
                _types.Add(type, typeDef);
            }

            return typeDef;
        }

        TypeDef GetExistingTypeDefOrNull(Type type)
        {
            return _types.ContainsKey(type) ? _types[type] : null;
        }

        TypeDef CreateTypeDef(QualifiedClassName qualifiedClassName, Type type)
        {
            var typeDef = IsCommand(type)
                ? new TypeDef(qualifiedClassName, GetTypeFor(typeof(Command)), TypeType.Command, type)
                : new TypeDef(qualifiedClassName, TypeType.Other);

            _types[type] = typeDef;

            foreach (var property in GetAllProperties(type))
            {
                var propertyDef =
                    new PropertyDef(GetOrCreateTypeDef(new QualifiedClassName(property.PropertyType), property.PropertyType),
                        property.Name);

                if (typeDef.TypeType == TypeType.Command && propertyDef.Name == "Meta") continue;

                typeDef.AddProperty(propertyDef);
            }

            return typeDef;
        }

        IEnumerable<PropertyInfo> GetAllProperties(Type type)
        {
            return type.GetProperties(BindingFlags.Instance | BindingFlags.Public);
        }

        public string GetCommandProcessorDefinitation()
        {
            var builder = new StringBuilder();

            builder.AppendLine(@"class CommandProcessor {
    private processCommandCallback: Function;

    constructor(processCommandCallback: Function) {
        this.processCommandCallback = processCommandCallback;
    }

    generateId() : Guid {
        var guid = (this.g() + this.g() + ""-"" + this.g() + ""-"" + this.g() + ""-"" + this.g() + ""-"" + this.g() + this.g() + this.g());
        
        return guid.toUpperCase();
    }
");

            foreach (
                var commandType in
                    _types.Values.Where(t => t.TypeType == TypeType.Command).OrderBy(t => t.FullyQualifiedTsTypeName))
            {
                builder.AppendLine(string.Format(@"    {0}(command: {1}) : void {{
        command[""$type""] = ""{2}"";
        this.invokeCallback(command);
    }}", ToCamelCase(commandType), commandType.FullyQualifiedTsTypeName, commandType.AssemblyQualifiedName));

                builder.AppendLine();
            }


            builder.AppendLine(@"    private invokeCallback(command: Command) : void {
        try {
            this.processCommandCallback(command);
        } catch(error) {
            console.log(""Command processing error"", error);
        }
    }

    private g() {
        return (((1 + Math.random()) * 0x10000) | 0).toString(16).substring(1);
    }
}");

            return builder.ToString();
        }

        static string ToCamelCase(TypeDef commandType)
        {
            var name = commandType.Name.Name;

            return char.ToLower(name[0]) + name.Substring(1);
        }

        public string GetCommandDefinitations()
        {
            var builder = new StringBuilder();

            var typeGroups = _types.Values
                .GroupBy(t => t.TypeType)
                .OrderBy(g => g.Key)
                .ToList();

            foreach (var typeGroup in typeGroups)
            {
                builder.AppendLine(string.Format(@"/* {0} */", FormatTypeType(typeGroup.Key)));

                foreach (var type in typeGroup)
                {
                    var code = type.GetCode(this);
                    if (String.IsNullOrWhiteSpace(code)) continue;

                    builder.AppendLine(code);
                    builder.AppendLine();
                }

                builder.AppendLine();
            }

            return builder.ToString();
        }

        string FormatTypeType(TypeType typeType)
        {
            switch (typeType)
            {
                case TypeType.Command:
                    return "Domain commands";

                case TypeType.Other:
                    return "Domain primitives";

                case TypeType.Primitive:
                    return "Built-in primitives";

                default:
                    return typeType.ToString();
            }
        }

        public TypeDef GetTypeFor(Type type)
        {
            var typeDef = GetExistingTypeDefOrNull(type);

            if (typeDef == null)
            {
                throw new ArgumentException(String.Format("Could not find type for {0}!", type));
            }

            return typeDef;
        }

        public static bool IsCommand(Type type)
        {
            if (type.IsAbstract) return false;
            if (type.IsInterface) return false;

            return HasCommandBaseClass(type);
        }

        static bool HasCommandBaseClass(Type type)
        {
            var baseType = type.BaseType;
            if (baseType == null) return false;

            if (baseType.Namespace == "d60.Cirqus.Commands" && baseType.Name == "Command")
                return true;

            return HasCommandBaseClass(baseType);
        }
    }
}
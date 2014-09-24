using System;
using System.Collections.Generic;
using System.Linq;

namespace d60.Cirqus.TsClient.Model
{
    class TypeDef
    {
        readonly TypeDef _baseType;
        readonly List<PropertyDef> _properties = new List<PropertyDef>();

        public TypeDef(QualifiedClassName name, TypeType typeType)
        {
            Name = name;
            TypeType = typeType;
        }

        public TypeDef(QualifiedClassName name, TypeDef baseType, TypeType typeType, Type type)
        {
            _baseType = baseType;
            Name = name;
            TypeType = typeType;
            Type = type;
        }

        public QualifiedClassName Name { get; private set; }

        public IEnumerable<PropertyDef> Properties
        {
            get { return _properties; }
        }

        public override string ToString()
        {
            return string.Format("{0} ({1} properties)", Name, _properties.Count);
        }

        public void AddProperty(PropertyDef propertyDef)
        {
            _properties.Add(propertyDef);
        }

        public virtual string FullyQualifiedTsTypeName
        {
            get { return string.Format("{0}.{1}", Name.Ns, Name.Name); }
        }

        public TypeType TypeType
        {
            get;
            private set;
        }

        public bool Optional { get; set; }
        public Type Type { get; protected set; }

        public string AssemblyQualifiedName
        {
            get
            {
                if (Type == null)
                {
                    return string.Format(".NET type has not been set on type def for {0}", Name);
                }
                if (Type.AssemblyQualifiedName == null)
                {
                    return string.Format(".NET type on type def for {0} seems to have NULL as its assembly qualified name", Name);
                }
                return string.Join(",", Type.AssemblyQualifiedName.Split(',').Take(2));
            }
        }

        public virtual string GetCode(ProxyGeneratorContext context)
        {
            return string.Format(@"declare module {0} {{
    interface {1}{2} {{
{3}
    }}
}}", Name.Ns, Name.Name, GetExtensionText(), FormatProperties());
        }

        string GetExtensionText()
        {
            if (_baseType == null) return "";

            return string.Format(" extends {0}", _baseType.FullyQualifiedTsTypeName);
        }

        string FormatProperties()
        {
            return string.Join(Environment.NewLine, Properties
                .Select(p => string.Format("        {0}{1}: {2};",
                    p.Name,
                    p.Type.Optional ? "?" : "",
                    p.Type.FullyQualifiedTsTypeName)));
        }
    }
}
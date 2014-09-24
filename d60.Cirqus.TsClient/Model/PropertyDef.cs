namespace d60.Cirqus.TsClient.Model
{
    class PropertyDef
    {
        readonly TypeDef _type;
        readonly string _name;

        public PropertyDef(TypeDef type, string name)
        {
            _type = type;
            _name = name;
        }

        public TypeDef Type
        {
            get { return _type; }
        }

        public string Name
        {
            get { return _name; }
        }
    }
}
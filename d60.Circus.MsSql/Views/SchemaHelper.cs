using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;

namespace d60.Circus.MsSql.Views
{
    public class SchemaHelper
    {
        static readonly Dictionary<Type, Tuple<SqlDbType, string>> DbTypes =
            new Dictionary<Type, Tuple<SqlDbType, string>>
            {
                {typeof (Guid), Tuple.Create(SqlDbType.UniqueIdentifier, "")},

                {typeof (short), Tuple.Create(SqlDbType.SmallInt, "")},
                {typeof (int), Tuple.Create(SqlDbType.Int, "")},
                {typeof (long), Tuple.Create(SqlDbType.BigInt, "")},

                {typeof (string), Tuple.Create(SqlDbType.NVarChar, "max")},
                
                {typeof (double), Tuple.Create(SqlDbType.Decimal, "12,5")},
                {typeof (float), Tuple.Create(SqlDbType.Decimal, "12,5")},
                {typeof (decimal), Tuple.Create(SqlDbType.Decimal, "12,5")},
                
                {typeof (List<string>), Tuple.Create(SqlDbType.NVarChar, "max")},
                {typeof (List<int>), Tuple.Create(SqlDbType.NVarChar, "max")},
                {typeof (HashSet<string>), Tuple.Create(SqlDbType.NVarChar, "max")},
                {typeof (HashSet<int>), Tuple.Create(SqlDbType.NVarChar, "max")},
            };

        public static Prop[] GetSchema<TView>()
        {
            return typeof(TView)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Select(p => new
                {
                    Property = p,
                    Attribute = p.GetCustomAttribute<ColumnAttribute>()
                })
                .Select(p =>
                {
                    var propertyInfo = p.Property;
                    var columnName = p.Attribute != null
                        ? p.Attribute.ColumnName
                        : propertyInfo.Name;

                    var sqlDbType = MapType(propertyInfo.PropertyType);
                    return new Prop
                    {
                        ColumnName = columnName,
                        SqlDbType = sqlDbType.Item1,
                        Size = sqlDbType.Item2,
                        Getter = instance => GetGetter(propertyInfo, instance),
                        Setter = (instance, value) => GetSetter(propertyInfo, instance, value)
                    };
                })
                .ToArray();
        }

        static void GetSetter(PropertyInfo propertyInfo, object instance, object value)
        {
            object valueToSet;

            if (propertyInfo.PropertyType == typeof(List<string>))
            {
                var tokens = ((string)value).Split(';');
                
                valueToSet = tokens.ToList();
            }
            else if (propertyInfo.PropertyType == typeof(List<int>))
            {
                var tokens = ((string)value).Split(';').Select(int.Parse);

                valueToSet = tokens.ToList();
            }
            else if (propertyInfo.PropertyType == typeof(HashSet<string>))
            {
                var tokens = ((string)value).Split(';');

                valueToSet = new HashSet<string>(tokens);
            }
            else if (propertyInfo.PropertyType == typeof(HashSet<int>))
            {
                var tokens = ((string)value).Split(';').Select(int.Parse);

                valueToSet = new HashSet<int>(tokens);
            }
            else
            {
                valueToSet = Convert.ChangeType(value, propertyInfo.PropertyType);
            }

            propertyInfo.SetValue(instance, valueToSet);
        }

        static object GetGetter(PropertyInfo propertyInfo, object instance)
        {
            if (propertyInfo.PropertyType == typeof(List<string>))
            {
                var stringList = (List<string>)propertyInfo.GetValue(instance);

                return string.Join(";", stringList);
            }

            if (propertyInfo.PropertyType == typeof(List<int>))
            {
                var stringList = (List<int>)propertyInfo.GetValue(instance);

                return string.Join(";", stringList);
            }

            if (propertyInfo.PropertyType == typeof(HashSet<string>))
            {
                var stringList = (HashSet<string>)propertyInfo.GetValue(instance);

                return string.Join(";", stringList);
            }

            if (propertyInfo.PropertyType == typeof(HashSet<int>))
            {
                var stringList = (HashSet<int>)propertyInfo.GetValue(instance);

                return string.Join(";", stringList);
            }

            return propertyInfo.GetValue(instance);
        }

        static Tuple<SqlDbType, string> MapType(Type propertyType)
        {
            try
            {
                return DbTypes[propertyType];
            }
            catch (Exception exception)
            {
                throw new ArgumentException(string.Format("Could not map .NET type {0} to a proper SqlDbType", propertyType), exception);
            }
        }
    }

    public class Prop
    {
        public bool IsPrimaryKey
        {
            get { return ColumnName.Equals("Id", StringComparison.InvariantCultureIgnoreCase); }
        }
        public string ColumnName { get; set; }
        public Func<object, object> Getter { get; set; }
        public Action<object, object> Setter { get; set; }
        public SqlDbType SqlDbType { get; set; }
        public string Size { get; set; }
        public string SqlParameterName
        {
            get { return "@" + ColumnName; }
        }

        public override string ToString()
        {
            return string.Format("{0} ({1})", ColumnName, SqlDbType);
        }
    }

}
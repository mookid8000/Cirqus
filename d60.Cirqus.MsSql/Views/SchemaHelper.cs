using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using d60.Cirqus.Serialization;

namespace d60.Cirqus.MsSql.Views
{
    class SchemaHelper
    {
        static readonly Dictionary<Type, Tuple<SqlDbType, string>> DbTypes =
            new Dictionary<Type, Tuple<SqlDbType, string>>
            {
                {typeof (Guid), Tuple.Create(SqlDbType.UniqueIdentifier, "")},
                {typeof (Guid?), Tuple.Create(SqlDbType.UniqueIdentifier, "")},

                {typeof (short), Tuple.Create(SqlDbType.SmallInt, "")},
                {typeof (int), Tuple.Create(SqlDbType.Int, "")},
                {typeof (long), Tuple.Create(SqlDbType.BigInt, "")},

                {typeof (short?), Tuple.Create(SqlDbType.SmallInt, "")},
                {typeof (int?), Tuple.Create(SqlDbType.Int, "")},
                {typeof (long?), Tuple.Create(SqlDbType.BigInt, "")},

                {typeof (double), Tuple.Create(SqlDbType.Decimal, "12,5")},
                {typeof (float), Tuple.Create(SqlDbType.Decimal, "12,5")},
                {typeof (decimal), Tuple.Create(SqlDbType.Decimal, "12,5")},

                {typeof (double?), Tuple.Create(SqlDbType.Decimal, "12,5")},
                {typeof (float?), Tuple.Create(SqlDbType.Decimal, "12,5")},
                {typeof (decimal?), Tuple.Create(SqlDbType.Decimal, "12,5")},

                {typeof (string), Tuple.Create(SqlDbType.NVarChar, "max")},
                
                {typeof (List<string>), Tuple.Create(SqlDbType.NVarChar, "max")},
                {typeof (List<int>), Tuple.Create(SqlDbType.NVarChar, "max")},
                {typeof (List<double>), Tuple.Create(SqlDbType.NVarChar, "max")},
                {typeof (List<decimal>), Tuple.Create(SqlDbType.NVarChar, "max")},
                {typeof (HashSet<string>), Tuple.Create(SqlDbType.NVarChar, "max")},
                {typeof (HashSet<int>), Tuple.Create(SqlDbType.NVarChar, "max")},
                {typeof (string[]), Tuple.Create(SqlDbType.NVarChar, "max")},
                
                {typeof (DateTime), Tuple.Create(SqlDbType.DateTime2, "")},
                {typeof (DateTimeOffset), Tuple.Create(SqlDbType.DateTimeOffset, "")},
                {typeof (TimeSpan), Tuple.Create(SqlDbType.BigInt, "")},
            };

        static readonly GenericSerializer Serializer = new GenericSerializer();

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
                        PropertyIsNullable = DetermineNullability(propertyInfo),
                        Getter = instance => GetGetter(propertyInfo, instance),
                        Setter = (instance, value) => GetSetter(propertyInfo, instance, value)
                    };
                })
                .ToArray();
        }

        static bool DetermineNullability(PropertyInfo propertyInfo)
        {
            return !propertyInfo.GetCustomAttributes<NotNullAttribute>().Any();
        }

        static void GetSetter(PropertyInfo propertyInfo, object instance, object value)
        {
            object valueToSet;

            if (propertyInfo.GetCustomAttributes<JsonAttribute>().Any())
            {
                var text = Serializer.Deserialize((string)value);

                valueToSet = text;
            }
            else
            {
                var propertyTypeToLookAt = propertyInfo.PropertyType;

                if (propertyTypeToLookAt.IsGenericType 
                    && propertyTypeToLookAt.GetGenericTypeDefinition() == typeof (Nullable<>))
                {
                    propertyTypeToLookAt = propertyTypeToLookAt.GetGenericArguments().Single();
                }

                // -- COLLECTIONS --------------------------------------------------
                if (propertyTypeToLookAt == typeof(List<string>))
                {
                    var tokens = ((string)value).Split(';');

                    valueToSet = tokens.ToList();
                }
                else if (propertyTypeToLookAt == typeof(List<int>))
                {
                    var tokens = ((string)value).Split(';').Select(int.Parse);

                    valueToSet = tokens.ToList();
                }
                else if (propertyTypeToLookAt == typeof(List<double>))
                {
                    var tokens = ((string)value).Split(';').Select(double.Parse);

                    valueToSet = tokens.ToList();
                }
                else if (propertyTypeToLookAt == typeof(List<decimal>))
                {
                    var tokens = ((string)value).Split(';').Select(decimal.Parse);

                    valueToSet = tokens.ToList();
                }
                else if (propertyTypeToLookAt == typeof(HashSet<string>))
                {
                    var tokens = ((string)value).Split(';');

                    valueToSet = new HashSet<string>(tokens);
                }
                else if (propertyTypeToLookAt == typeof(HashSet<int>))
                {
                    var tokens = ((string)value).Split(';').Select(int.Parse);

                    valueToSet = new HashSet<int>(tokens);
                }
                else if (propertyTypeToLookAt == typeof(string[]))
                {
                    var tokens = ((string)value).Split(';');

                    valueToSet = tokens.ToArray();
                }
                // -- ANY KIND OF NULL PRIMITIVE VALUE --------------------------------------------------
                else if (value == DBNull.Value)
                {
                    valueToSet = null;
                }
                // -- SPECIAL PRIMITIVES --------------------------------------------------
                else if (propertyTypeToLookAt == typeof(DateTime))
                {
                    valueToSet = value;
                }
                else if (propertyTypeToLookAt == typeof(DateTimeOffset))
                {
                    valueToSet = value;
                }
                else if (propertyTypeToLookAt == typeof(TimeSpan))
                {
                    var ticks = (long)value;
                    valueToSet = new TimeSpan(ticks);
                }
                else if (propertyTypeToLookAt == typeof(Guid))
                {
                    valueToSet = (Guid) value;
                }
                else
                {
                    valueToSet = Convert.ChangeType(value, propertyTypeToLookAt);
                }
            }

            propertyInfo.SetValue(instance, valueToSet);
        }

        static object GetGetter(PropertyInfo propertyInfo, object instance)
        {
            var value = propertyInfo.GetValue(instance);

            if (propertyInfo.GetCustomAttributes<JsonAttribute>().Any())
            {
                var text = Serializer.Serialize(value);

                return text;
            }

            if (propertyInfo.PropertyType == typeof(List<string>))
            {
                var stringList = (List<string>)value;

                return string.Join(";", stringList);
            }

            if (propertyInfo.PropertyType == typeof(List<int>))
            {
                var stringList = (List<int>)value;

                return string.Join(";", stringList);
            }

            if (propertyInfo.PropertyType == typeof(List<double>))
            {
                var stringList = (List<double>)value;

                return string.Join(";", stringList);
            }

            if (propertyInfo.PropertyType == typeof(List<decimal>))
            {
                var stringList = (List<decimal>)value;

                return string.Join(";", stringList);
            }

            if (propertyInfo.PropertyType == typeof(HashSet<string>))
            {
                var stringList = (HashSet<string>)value;

                return string.Join(";", stringList);
            }

            if (propertyInfo.PropertyType == typeof(HashSet<int>))
            {
                var stringList = (HashSet<int>)value;

                return string.Join(";", stringList);
            }

            if (propertyInfo.PropertyType == typeof(string[]))
            {
                var stringList = (string[])value;

                return string.Join(";", stringList);
            }

            if (propertyInfo.PropertyType == typeof(DateTime))
            {
                return ((DateTime)value).ToUniversalTime();
            }

            if (propertyInfo.PropertyType == typeof(DateTimeOffset))
            {
                return (DateTimeOffset)value;
            }

            if (propertyInfo.PropertyType == typeof(TimeSpan))
            {
                return ((TimeSpan)value).Ticks;
            }

            return value;
        }

        static Tuple<SqlDbType, string> MapType(Type propertyType)
        {
            return DbTypes.ContainsKey(propertyType)
                ? DbTypes[propertyType]
                : Tuple.Create(SqlDbType.NVarChar, "max");
        }
    }

    class   Prop
    {
        public bool IsPrimaryKey
        {
            get { return ColumnName.Equals("Id", StringComparison.InvariantCultureIgnoreCase); }
        }

        public bool IsGlobalSequenceNumber
        {
            get { return ColumnName.Equals("LastGlobalSequenceNumber", StringComparison.InvariantCultureIgnoreCase); }
        }

        public bool Matches(Prop otherProp)
        {
            return ColumnName.Equals(otherProp.ColumnName, StringComparison.InvariantCultureIgnoreCase)
                   && SqlDbType == otherProp.SqlDbType;
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

        public bool IsNullable
        {
            get
            {
                return !IsPrimaryKey
                       && !IsGlobalSequenceNumber
                       && PropertyIsNullable;
            }
        }

        public bool PropertyIsNullable { get; set; }

        public override string ToString()
        {
            return string.Format("{0} ({1})", ColumnName, SqlDbType);
        }
    }

}
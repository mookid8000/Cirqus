using System;

namespace d60.Cirqus.MsSql.Projections.Views
{
    /// <summary>
    /// Apply to a property to explicitly set the column name used in the database.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class ColumnAttribute : Attribute
    {
        public string ColumnName { get; private set; }

        public ColumnAttribute(string columnName)
        {
            ColumnName = columnName;
        }
    }
}
using System;

namespace d60.Cirqus.MsSql.Views
{
    /// <summary>
    /// Apply to a property on a view model to be managed by <see cref="MsSqlViewManager{TViewInstance}"/> in order for that
    /// property to be serialized as JSON to an NVARCHAR(MAX) column.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class JsonAttribute : Attribute
    {
    }
}
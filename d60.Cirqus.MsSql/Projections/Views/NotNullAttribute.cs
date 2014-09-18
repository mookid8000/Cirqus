using System;

namespace d60.Cirqus.MsSql.Projections.Views
{
    /// <summary>
    /// Apply to a property on a view model to be managed by <see cref="MsSqlViewManager{TViewInstance}"/> in order for that
    /// property to be saved to a non-nullable column in the database. WARNING: This means that the view will halt if it ever
    /// encounters a NULL value on this property.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class NotNullAttribute : Attribute
    {
    }
}
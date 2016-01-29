using System;

namespace d60.Cirqus.Snapshotting
{
    /// <summary>
    /// Indicates that this particular aggregate root type can have snapshots taken of it. NOTE: Please remember to update
    /// the version in case you change ANYTHING about the data in the aggregate root type.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class EnableSnapshotsAttribute : Attribute
    {
        /// <summary>
        /// Gets the version
        /// </summary>
        public int Version { get; private set; }

        /// <summary>
        /// Indicates that the aggregate root type has the <paramref name="version"/> version. Remember to increment this value
        /// each time anything about the private data in the aggregate root type changes.
        /// </summary>
        public EnableSnapshotsAttribute(int version)
        {
            Version = version;
        }
    }
}
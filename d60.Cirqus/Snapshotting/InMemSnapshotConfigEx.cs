using System;
using d60.Cirqus.Snapshotting.New;

namespace d60.Cirqus.Snapshotting
{
    /// <summary>
    /// Configuration extensions for enabling in-mem aggregate root snapshots
    /// </summary>
    public static class InMemSnapshotConfigEx
    {
        /// <summary>
        /// Configures Cirqus to use RAM to store aggregate root snapshots
        /// </summary>
        public static void UseInMemorySnapshotStore(this SnapshottingConfigurationBuilder builder)
        {
            if (builder == null) throw new ArgumentNullException("builder");

            builder.Register(c => new InMemorySnapshotStore());
        }
    }
}
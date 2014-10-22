using System;
using d60.Cirqus.Config.Configurers;
using d60.Cirqus.Events;
using d60.Cirqus.Ntfs.Events;

namespace d60.Cirqus.Ntfs.Config
{
    public static class NtfsConfigurationExtensions
    {
        public static void UseFiles(this EventStoreConfigurationBuilder builder, string path)
        {
            if (builder == null) throw new ArgumentNullException("builder");
            if (string.IsNullOrEmpty(path)) throw new ArgumentNullException("path");
            
            builder.Registrar.Register<IEventStore>(context => new NtfsEventStore(path));
        }
    }
}
using d60.Cirqus.Aggregates;

namespace d60.Cirqus.Config.Configurers
{
    public class AggregateRootRepositoryConfigurationBuilder : ConfigurationBuilder<IAggregateRootRepository>
    {
        public AggregateRootRepositoryConfigurationBuilder(IRegistrar registrar) : base(registrar) { }
    }
}
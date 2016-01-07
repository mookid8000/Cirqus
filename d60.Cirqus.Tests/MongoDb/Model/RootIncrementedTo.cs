using d60.Cirqus.Events;

namespace d60.Cirqus.Tests.MongoDb.Model
{
    public class RootIncrementedTo : DomainEvent<Root>
    {
        public int Number { get; }

        public RootIncrementedTo(int number)
        {
            Number = number;
        }
    }
}
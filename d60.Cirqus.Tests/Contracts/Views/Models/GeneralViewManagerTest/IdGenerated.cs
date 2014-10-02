using d60.Cirqus.Events;

namespace d60.Cirqus.Tests.Contracts.Views.Models.GeneralViewManagerTest
{
    public class IdGenerated : DomainEvent<IdGenerator>
    {
        public int Pointer { get; set; }

        public string IdBase { get; set; }

        public string GetId()
        {
            return string.Format("{0}/{1}", IdBase, Pointer);
        }
    }
}
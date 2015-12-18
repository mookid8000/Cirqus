using System.Collections.Generic;
using d60.Cirqus.Events;

namespace d60.Cirqus.Views
{
    /// <summary>
    /// Guess what this bad boy is doing? NOTHING, that's what!
    /// </summary>
    class NullEventDispatcher : IEventDispatcher
    {
        public void Initialize(bool purgeExistingViews = false)
        {
        }

        public void Dispatch(IEnumerable<DomainEvent> events)
        {
        }
    }
}
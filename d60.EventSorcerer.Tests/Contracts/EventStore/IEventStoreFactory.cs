using d60.EventSorcerer.Events;
using d60.EventSorcerer.Numbers;

namespace d60.EventSorcerer.Tests.Contracts.EventStore
{
    public interface IEventStoreFactory
    {
        IEventStore GetEventStore();
        ISequenceNumberGenerator GetSequenceNumberGenerator();
    }
}
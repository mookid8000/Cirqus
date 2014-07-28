using System;

namespace d60.EventSorcerer.Numbers
{
    /// <summary>
    /// Implement this to provide logic on how stuff is sequentially numbered
    /// </summary>
    public interface ISequenceNumberGenerator
    {
        int Next(Guid aggregateRootId);
    }
}
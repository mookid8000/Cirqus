using System;

namespace d60.EventSorcerer.Numbers
{
    /// <summary>
    /// Implement this to provide logic on how stuff is sequentially numbered. Sequence number generators must ALWAYS start with the number 0 for each
    /// unique aggregate root ID
    /// </summary>
    public interface ISequenceNumberGenerator
    {
        long Next(Guid aggregateRootId);
    }
}
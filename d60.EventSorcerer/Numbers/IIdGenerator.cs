namespace d60.EventSorcerer.Numbers
{
    /// <summary>
    /// Implement this in order to provide logic on how IDs are generated
    /// </summary>
    public interface IIdGenerator
    {
        int GenerateId();
    }
}
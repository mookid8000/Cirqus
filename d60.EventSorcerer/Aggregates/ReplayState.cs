namespace d60.EventSorcerer.Aggregates
{
    internal enum ReplayState
    {
        None,
        EmitApply,
        ReplayApply,
    }
}
namespace d60.Circus.Aggregates
{
    internal enum ReplayState
    {
        None,
        EmitApply,
        ReplayApply,
    }
}
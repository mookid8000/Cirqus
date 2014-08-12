namespace d60.Circus.Events
{
    /// <summary>
    /// Marks an aggregate root as an emitter (and thus also as a consumer) of this particular type of domain event
    /// </summary>
    public interface IEmit<TDomainEvent> where TDomainEvent : DomainEvent
    {
        /// <summary>
        /// Applies changes to the aggregate root instance that happens as a consequence of this event
        /// </summary>
        void Apply(TDomainEvent e);
    }
}
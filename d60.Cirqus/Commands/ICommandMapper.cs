using System;
using d60.Cirqus.Aggregates;

namespace d60.Cirqus.Commands
{
    ///// <summary>
    ///// Holds a configuration on how to map an incoming command to (preferably) one or more (_can_ be done) operations on an aggregate root
    ///// </summary>
    //public interface ICommandMapper
    //{
    //    /// <summary>
    //    /// Gets a handler method that can be invoked with a command and the aggregate root instance that was
    //    /// decided to be the correct recipient of the command
    //    /// </summary>
    //    Action<TCommand, TAggregateRoot> GetHandlerFor<TCommand, TAggregateRoot>()
    //        where TCommand : Command<TAggregateRoot>
    //        where TAggregateRoot : AggregateRoot;
    //}
}
using System;
using d60.Cirqus.Aggregates;
using d60.Cirqus.Numbers;
using d60.Cirqus.Testing;

namespace d60.Cirqus.Commands
{
    /// <summary>
    /// Ultimate command base class. When you use this class to create your commands, you need to provide
    /// an action mapping on the current <see cref="DefaultCommandMapper"/>. Use <see cref="ExecutableCommand"/>
    /// if you want to skip the action mapping and provide an executable command, or <see cref="Command{TAggregateRoot}"/> 
    /// if you intend to address one single aggregate root instance only (which you probably should in most cases)
    /// </summary>
    public abstract class Command
    {
        protected Command()
        {
            Meta = new Metadata();
        }

        public Metadata Meta { get; private set; }
    }

    /// <summary>
    /// Executable command base class that avoids the need for providing an action mapping by requiring the
    /// action to be implemented as part of the command - use <see cref="Command{TAggregateRoot}"/> if you intend to address one single 
    /// aggregate root instance only (which you probably should in most cases)
    /// </summary>
    public abstract class ExecutableCommand : Command
    {
        public abstract void Execute(ICommandContext context);
    }

    /// <summary>
    /// Command base class that is mapped to one specific aggregate root instance for which the <seealso cref="Execute(TAggregateRoot)"/> method will be invoked
    /// </summary>
    /// <typeparam name="TAggregateRoot">Specifies the type of aggregate root that this command targets</typeparam>
    public abstract class Command<TAggregateRoot> : ExecutableCommand where TAggregateRoot : AggregateRoot, new()
    {
        protected Command(string aggregateRootId)
        {
            if (aggregateRootId == null) throw new ArgumentNullException("aggregateRootId", "You need to specify an aggregate root ID");

            AggregateRootId = aggregateRootId;
        }

        public string AggregateRootId { get; private set; }

        public sealed override void Execute(ICommandContext context)
        {
            var aggregateRootInstance = context.Load<TAggregateRoot>(AggregateRootId, createIfNotExists: true);

            Execute(aggregateRootInstance);
        }

        public abstract void Execute(TAggregateRoot aggregateRoot);

        public override string ToString()
        {
            return string.Format("{0} => {1} {2}", GetType().Name, typeof(TAggregateRoot).Name, AggregateRootId);
        }
    }
}
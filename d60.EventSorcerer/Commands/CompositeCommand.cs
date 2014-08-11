using System;
using System.Collections.Generic;
using System.Linq;
using d60.EventSorcerer.Aggregates;

namespace d60.EventSorcerer.Commands
{
    /// <summary>
    /// Composite command that allows for packing up multiple commands and have them executed in one single unit of work
    /// </summary>
    public class CompositeCommand<TAggregateRoot> : MappedCommand<TAggregateRoot> where TAggregateRoot : AggregateRoot, new()
    {
        public List<MappedCommand<TAggregateRoot>> Commands { get; set; }

        public CompositeCommand(params MappedCommand<TAggregateRoot>[] commands)
            : base(commands.First().AggregateRootId)
        {
            var addressedAggregateRoots = commands.Select(c => c.AggregateRootId).Distinct().ToList();

            if (addressedAggregateRoots.Count > 1)
            {
                throw new ArgumentException(
                    string.Format(
                        "Cannot address more than one single aggregate root instance with a composite command - the following aggregate root IDs were addressed: {0}",
                        string.Join(", ", addressedAggregateRoots)));
            }

            Commands = commands.ToList();
        }

        public override void Execute(TAggregateRoot aggregateRoot)
        {
            foreach (var command in Commands)
            {
                command.Execute(aggregateRoot);
            }
        }
    }

    public class CompositeCommand
    {
        public static CompositeCommandBuilder<TAggregateRoot> For<TAggregateRoot>()
            where TAggregateRoot : AggregateRoot, new()
        {
            return new CompositeCommandBuilder<TAggregateRoot>();
        }
    }

    public class CompositeCommandBuilder<TAggregateRoot> where TAggregateRoot : AggregateRoot, new()
    {
        readonly List<MappedCommand<TAggregateRoot>> _commands = new List<MappedCommand<TAggregateRoot>>();

        public CompositeCommandBuilder<TAggregateRoot> With(MappedCommand<TAggregateRoot> command)
        {
            _commands.Add(command);
            return this;
        }

        public static implicit operator CompositeCommand<TAggregateRoot>(CompositeCommandBuilder<TAggregateRoot> builder)
        {
            return new CompositeCommand<TAggregateRoot>(builder._commands.ToArray());
        }
    }
}
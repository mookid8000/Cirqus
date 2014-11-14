using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using d60.Cirqus.Aggregates;
using d60.Cirqus.Commands;
using d60.Cirqus.Config;
using d60.Cirqus.Events;
using d60.Cirqus.Testing.Internals;
using d60.Cirqus.Tests.Extensions;
using NUnit.Framework;

namespace d60.Cirqus.Tests.Integration
{
    [TestFixture]
    public class TypeNameAliases : FixtureBase
    {
        ICommandProcessor _commandProcessor;
        Task<InMemoryEventStore> _eventStore;
        MyNameMapper _nameMapper;

        protected override void DoSetUp()
        {
            _nameMapper = new MyNameMapper();

            _commandProcessor = CommandProcessor.With()
                .EventStore(e => _eventStore = e.UseInMemoryEventStore())
                .Options(o => o.UseCustomAggregateRootTypeMapper(_nameMapper))
                .Create();

            RegisterForDisposal(_commandProcessor);
        }

        [Test]
        public void SetsOwnerToGivenAlias()
        {
            const string alias = "I_am_calling_it_something_else";

            _nameMapper.AddAlias<OneRoot>(alias);

            var command = new GenericCommand(context =>
            {
                var oneRoot = context.Load<OneRoot>("someId", createIfNotExists: true);
                oneRoot.Bam();
            });

            _commandProcessor.ProcessCommand(command);

            var metadataOfEvent = _eventStore.Result.Single().Meta;

            Assert.That(metadataOfEvent[DomainEvent.MetadataKeys.Owner], Is.EqualTo(alias));
        }

        class OneRoot : AggregateRoot, IEmit<OneEvent>
        {
            public void Bam()
            {
                Emit(new OneEvent());
            }

            public void Apply(OneEvent e)
            {
            }
        }

        class OneEvent : DomainEvent<OneRoot> { }

        class GenericCommand : ExecutableCommand
        {
            readonly Action<ICommandContext> _action;

            public GenericCommand(Action<ICommandContext> action)
            {
                _action = action;
            }

            public override void Execute(ICommandContext context)
            {
                _action(context);
            }
        }
    }

    public class MyNameMapper : IAggregateRootTypeMapper
    {
        readonly ConcurrentDictionary<string, Type> _aliasToType = new ConcurrentDictionary<string, Type>();
        readonly ConcurrentDictionary<Type, string> _typeToAlias = new ConcurrentDictionary<Type, string>();

        public void AddAlias<TAggregateRoot>(string alias) where TAggregateRoot : AggregateRoot
        {
            var type = typeof(TAggregateRoot);
            _aliasToType[alias] = type;
            _typeToAlias[type] = alias;
        }

        public Type GetType(string name)
        {
            try
            {
                return _aliasToType[name];

            }
            catch (Exception exception)
            {
                throw new ArgumentException(string.Format("Could not get type for {0}", name), exception);
            }
        }

        public string GetName(Type type)
        {
            try
            {
                return _typeToAlias[type];

            }
            catch (Exception exception)
            {
                throw new ArgumentException(string.Format("Could not get name for {0}", type), exception);
            }
        }
    }
}
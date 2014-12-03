using System;
using System.Collections.Generic;
using System.Linq;
using d60.Cirqus.Aggregates;
using d60.Cirqus.Commands;
using d60.Cirqus.Events;
using d60.Cirqus.Tests.Extensions;
using NUnit.Framework;

namespace d60.Cirqus.Tests.Integration
{
    [TestFixture]
    public class LoadAggregateRootBySuperclass : FixtureBase
    {
        ICommandProcessor _commandProcessor;

        protected override void DoSetUp()
        {
            _commandProcessor = CommandProcessor.With()
                .EventStore(e => e.UseInMemoryEventStore())
                .Create();

            RegisterForDisposal(_commandProcessor);
        }

        [Test]
        public void CanLoadAggregateRootByVariousSuperclassesAndInterfaces()
        {
            const string aggregateRootId = "AnAggregateRoot/1";
            var command = new GenericCommand(ctx =>
            {
                var aggregateRoot = ctx.Create<AnAggregateRoot>(aggregateRootId);

                aggregateRoot.ComeIntoExistence();
            });
            _commandProcessor.ProcessCommand(command);

            var loadedObjects = new List<object>();

            var bigLoada = new GenericCommand(ctx =>
            {
                var ordinaryLoad = ctx.Load<AnAggregateRoot>(aggregateRootId);
                var loadByUnrelatedInterface = ctx.Load<ISomeUnrelatedInterface>(aggregateRootId);
                var loadByBaseclass = ctx.Load<AggregateRoot>(aggregateRootId);
                var loadByUltimateBaseclass = ctx.Load<object>(aggregateRootId);

                loadedObjects.Add(ordinaryLoad);
                loadedObjects.Add(loadByUnrelatedInterface);
                loadedObjects.Add(loadByBaseclass);
                loadedObjects.Add(loadByUltimateBaseclass);
            });

            _commandProcessor.ProcessCommand(bigLoada);

            Assert.That(loadedObjects.Cast<AggregateRoot>().All(o => o.Id == aggregateRootId));

            var firstInstance = loadedObjects.First();
            Assert.That(loadedObjects.All(obj => ReferenceEquals(obj, firstInstance)));
        }

        class GenericCommand : ExecutableCommand
        {
            readonly Action<ICommandContext> _whatToDo;

            public GenericCommand(Action<ICommandContext> whatToDo)
            {
                _whatToDo = whatToDo;
            }

            public override void Execute(ICommandContext context)
            {
                _whatToDo(context);
            }
        }

        class AnAggregateRoot : AggregateRoot, ISomeUnrelatedInterface, IEmit<AnEvent>
        {
            string _data;

            public string Data
            {
                get { return _data; }
            }

            public void ComeIntoExistence()
            {
                Emit(new AnEvent());
            }

            public void Apply(AnEvent e)
            {
                _data = "yeah, I'm here";
            }
        }

        class AnEvent : DomainEvent<AnAggregateRoot> { }

        internal interface ISomeUnrelatedInterface
        {
        }
    }

}
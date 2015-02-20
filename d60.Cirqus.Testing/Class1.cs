using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using d60.Cirqus.Aggregates;
using d60.Cirqus.Events;
using d60.Cirqus.Serialization;
using EnergyProjects.Domain.Utilities;
using EnergyProjects.Tests;

namespace d60.Cirqus.Testing
{
    public abstract class CirqusTestsHarness
    {
        protected const string checkmark = "\u221A";
        protected const string cross = "\u2717";

        Stack<TempId> ids;

        readonly Func<IWriter> newWriter;

        protected IDomainEventSerializer eventSerializer;
        protected TestContext context;
        protected IWriter writer;

        protected CirqusTestsHarness(Func<IWriter> newWriter)
        {
            this.newWriter = newWriter;
        }

        protected void Begin()
        {
            ids = new Stack<TempId>();

            eventSerializer = new JsonDomainEventSerializer();
            context = TestContext.With()
                .Options(x => x.UseCustomDomainEventSerializer(eventSerializer))
                .Create();

            writer = newWriter();
        }

        protected void Emit<T>(DomainEvent<T> @event) where T : AggregateRoot
        {
            Emit(Latest<T>(), @event);
        }

        protected void Emit<T>(string id, params DomainEvent<T>[] events) where T : AggregateRoot
        {
            foreach (var @event in events)
            {
                Emit(id, @event);
            }
        }

        void Emit<T>(string id, DomainEvent<T> @event) where T : AggregateRoot
        {
            @event.Meta[DomainEvent.MetadataKeys.AggregateRootId] = id;
            ids.Push(new TempId<T>(id));
            context.Save(@event);

            writer
                .Block("Given that:")
                .Write(@event)
                .NewLine()
                .NewLine();
        }

        protected string NewId<T>()
        {
            var id = Guid.NewGuid().ToString();
            ids.Push(new TempId<T>(id));
            return id;
        }

        protected string Id<T>() where T : class
        {
            return Id<T>(1);
        }

        protected string Id<T>(int index) where T : class
        {
            var array = ids.OfType<TempId<T>>().Reverse().ToArray();
            if (array.Length < index)
            {
                throw new IndexOutOfRangeException(String.Format("Could not find Id<{0}> with index {1}", typeof(T).Name, index));
            }

            return array[index - 1].ToString();
        }

        protected string Latest<T>() where T : AggregateRoot
        {
            return ids.OfType<TempId<T>>().First().ToString();
        }

        interface TempId
        {
             
        }

        class TempId<T> : TempId
        {
            readonly string id;

            public TempId(string id)
            {
                this.id = id;
            }

            public override string ToString()
            {
                return id;
            }
        }
    }

}

using System;
using System.Collections.Generic;
using System.Linq;
using d60.Cirqus.Aggregates;
using d60.Cirqus.Events;
using d60.Cirqus.Serialization;
using EnergyProjects.Domain.Utilities;

namespace d60.Cirqus.Testing
{
    public class CirqusTestsHarness
    {
        readonly Func<IWriter> newWriter;
        protected const string checkmark = "\u221A";
        protected const string cross = "\u2717";

        Stack<TempId> ids;
        IWriter writer;
        TestContext context;

        JsonDomainEventSerializer eventSerializer;

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

            //SetupAuthenticationMetadata(@event.Meta);

            context.Save(@event);

            writer
                .Block("Given that:")
                .Write(@event)
                .NewLine()
                .NewLine();
        }

        protected string NewId<T>(params object[] args)
        {
            var id = Guid.NewGuid().ToString();
            ids.Push(new TypedId<T>(id));
            return id;
        }

        protected string Id<T>() where T : class
        {
            return Id<T>(1);
        }

        protected string Id<T>(int index) where T : class
        {
            var array = ids.OfType<TypedId<T>>().Reverse().ToArray();
            if (array.Length < index)
            {
                throw new IndexOutOfRangeException(String.Format("Could not find Id<{0}> with index {1}", typeof(T).Name, index));
            }

            return array[index - 1].ToString();
        }

        protected string Latest<T>() where T : AggregateRoot
        {
            string id;
            if (!TryGetLatest<T>(out id))
                throw new InvalidOperationException(string.Format("Can not get latest {0} id, since none exists.", typeof(T).Name));

            return id;
        }

        protected bool TryGetLatest<T>(out string latest) where T : AggregateRoot
        {
            latest = null;

            var lastestOfType = ids.OfType<TypedId<T>>().ToList();
            if (lastestOfType.Any())
            {
                latest = lastestOfType.First().ToString();
                return true;
            }

            return false;
        }

        //protected void SetupAuthenticationMetadata(Metadata meta)
        //{
        //    Id<Company> latestCompanyId;
        //    if (TryGetLatest(out latestCompanyId))
        //        meta[MetadataEx.CompanyIdMetadataKey] = latestCompanyId.ToString();

        //    Id<User> latestUserId;
        //    if (TryGetLatest(out latestUserId))
        //        meta[MetadataEx.UserIdMetadataKey] = latestUserId.ToString();
        //}


        protected interface TempId
        {
             
        }

        class TypedId<T> : TempId
        {
            readonly string id;

            public TypedId(string id)
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

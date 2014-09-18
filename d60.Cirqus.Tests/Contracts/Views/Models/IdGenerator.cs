using System;
using System.Collections.Generic;
using d60.Cirqus.Aggregates;
using d60.Cirqus.Commands;
using d60.Cirqus.Events;
using d60.Cirqus.Views.ViewManagers;
using d60.Cirqus.Views.ViewManagers.Locators;

namespace d60.Cirqus.Tests.Contracts.Views.Models
{
    public class IdGenerator : AggregateRoot, IEmit<IdGenerated>
    {
        public static readonly Guid InstanceId = new Guid("417B504A-A7F5-4AC8-8005-6D85133D53DF");

        readonly Dictionary<string, int> _pointersByIdBase = new Dictionary<string, int>();

        public string GenerateNewId(string idBase)
        {
            var pointer = _pointersByIdBase.ContainsKey(idBase)
                ? _pointersByIdBase[idBase] + 1
                : 0;

            var evt = new IdGenerated
            {
                IdBase = idBase,
                Pointer = pointer
            };

            Emit(evt);

            return evt.GetId();
        }

        public void Apply(IdGenerated e)
        {
            _pointersByIdBase[e.IdBase] = e.Pointer;
        }
    }

    public class IdGenerated : DomainEvent<IdGenerator>
    {
        public int Pointer { get; set; }

        public string IdBase { get; set; }

        public string GetId()
        {
            return string.Format("{0}/{1}", IdBase, Pointer);
        }
    }

    public class GenerateNewId : Command<IdGenerator>
    {
        public GenerateNewId(Guid aggregateRootId)
            : base(aggregateRootId)
        {
        }

        public string IdBase { get; set; }

        public override void Execute(IdGenerator aggregateRoot)
        {
            aggregateRoot.GenerateNewId(IdBase);
        }
    }

    public class GeneratedIds : IViewInstance<InstancePerAggregateRootLocator>, ISubscribeTo<IdGenerated>
    {
        public string Id { get; set; }
        public long LastGlobalSequenceNumber { get; set; }

        public GeneratedIds()
        {
            AllIds = new HashSet<string>();
        }

        public HashSet<string> AllIds { get; set; }
        
        public void Handle(IViewContext context, IdGenerated domainEvent)
        {
            Console.WriteLine("=============== Adding ID: {0} ===============", domainEvent.GetId());

            AllIds.Add(domainEvent.GetId());
        }
    }
}
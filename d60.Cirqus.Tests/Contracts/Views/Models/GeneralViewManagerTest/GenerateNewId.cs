using System;
using d60.Cirqus.Commands;

namespace d60.Cirqus.Tests.Contracts.Views.Models.GeneralViewManagerTest
{
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
}
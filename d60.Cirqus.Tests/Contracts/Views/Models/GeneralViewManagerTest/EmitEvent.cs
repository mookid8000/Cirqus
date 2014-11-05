using System;
using d60.Cirqus.Commands;

namespace d60.Cirqus.Tests.Contracts.Views.Models.GeneralViewManagerTest
{
    public class EmitEvent : Command<EventEmitter>
    {
        public EmitEvent(string aggregateRootId)
            : base(aggregateRootId)
        {
        }

        public override void Execute(EventEmitter aggregateRoot)
        {
            aggregateRoot.DoIt();
        }
    }
}
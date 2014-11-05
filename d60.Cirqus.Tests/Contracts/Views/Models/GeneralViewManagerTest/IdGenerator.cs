using System.Collections.Generic;
using d60.Cirqus.Aggregates;
using d60.Cirqus.Events;

namespace d60.Cirqus.Tests.Contracts.Views.Models.GeneralViewManagerTest
{
    public class IdGenerator : AggregateRoot, IEmit<IdGenerated>
    {
        public static readonly string InstanceId = "IdGenerator";

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
}
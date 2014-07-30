using System;
using System.Collections.Generic;

namespace d60.EventSorcerer.Views.Basic
{
    public interface IView
    {
        string Id { get; set; }
    }

    public interface IView<TViewLocator> : IView where TViewLocator : ViewLocator
    {
    }

    public abstract class CatchUpView : IView {
        public string Id { get; set; }
    }

    public abstract class CatchUpView<TViewLocator> : CatchUpView where TViewLocator : ViewLocator
    {
        protected CatchUpView()
        {
            Pointers = new Dictionary<string, int>();
        }
        public Dictionary<string, int> Pointers { get; set; }
        internal void UpdatePointer(Guid aggId, int seqNo)
        {
            var id = aggId.ToString();

            if (!Pointers.ContainsKey(id))
            {
                Pointers[id] = seqNo;
                return;
            }


        }
    }
}
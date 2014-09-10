using d60.Cirqus.Views.ViewManagers;

namespace d60.Cirqus.MsSql.Views
{
    class MsSqlView<TViewInstance> where TViewInstance : IViewInstance
    {
        public TViewInstance View { get; set; }
        
        public long MaxGlobalSeq { get { return View.LastGlobalSequenceNumber; }}
    }
}